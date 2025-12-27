using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 敵ターンの処理 (攻撃のキューイングと実行、防御判定の委譲)
///</summary>
public class EnemyTurn : MonoBehaviour
{
    public event System.Action TurnFinished;

    [Header("Component References")]
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private BattleEntitiesManager entitiesManager;
    [SerializeField] private PlayerDefenseHandler defenseHandler;

    private List<PlayerRuntime> players;
    private List<EnemyModel> enemies;
    private List<EnemyRuntime> _enemyRuntimes;
    private Queue<ICommand> commandQueue = new();
    private List<PlayerStatusUIController> playerStatusUIControllers;
    private Dictionary<EnemyModel, EnemyController> enemyControllers;
    private Dictionary<PlayerRuntime, PlayerController> playerControllers;

    private PlayerRuntime currentPlayerTarget;
    private EnemyAttackCommand currentAttackCommand;
    private bool attackHasBeenResolved;

    private void Awake()
    {
        defenseHandler.OnDefenseResultFeedback += battleManager.ShowDefenseFeedback;
        defenseHandler.OnDamageToPlayer += HandleDamageToPlayer;
    }

    public void EnemySetup(List<PlayerRuntime> players, List<EnemyModel> enemys,
                       Dictionary<EnemyModel, EnemyController> enemyControllers,
                       Dictionary<PlayerRuntime, PlayerController> playerControllers,
                       List<PlayerStatusUIController> playerStatusUIControllers,
                        List<EnemyRuntime> runtimeList)
    {
        this.players = players;
        this.enemies = enemys;
        this.enemyControllers = enemyControllers;
        this.playerControllers = playerControllers;
        this.playerStatusUIControllers = playerStatusUIControllers;
        this._enemyRuntimes = runtimeList;

        defenseHandler.Init(players, playerControllers);

        foreach (var enemyController in this.enemyControllers.Values)
        {
            if (enemyController != null)
            {
                // アニメーションイベントの購読を、DefenseHandlerのメソッドに繋ぎ直す
                enemyController.OnAttackHitMoment += () =>
                {
                    // 攻撃がヒットする瞬間に、防御判定を開始し、完了時にResolveAttackを呼ぶコールバックを渡す
                    StartCoroutine(defenseHandler.StartDefenseWindowCoroutine(currentPlayerTarget, ResolveAttack));
                };
            }
        }
    }

    public void StartEnemyTurn()
    {
        commandQueue.Clear();
        defenseHandler.EnableDefenseInput();
        StartCoroutine(ProcessEnemyActions());
    }

    /// <summary>
    /// 攻撃対象となる生存プレイヤーをランダムに選択する
    /// </summary>
    private PlayerRuntime GetRandomLivingPlayer()
    {
        var livingPlayers = players.Where(p => p != null && p.CurrentHP > 0).ToList();
        if (livingPlayers.Any())
        {
            int choice = Random.Range(0, livingPlayers.Count);
            return livingPlayers[choice];
        }
        return null;
    }

    private void PrepareAttackCommands()
    {
        foreach (var attackerRuntime in _enemyRuntimes)
        {
            if (attackerRuntime == null || attackerRuntime.CurrentHP <= 0) continue;

            PlayerRuntime targetRuntime = GetRandomLivingPlayer();
            if (targetRuntime != null)
            {
                EnemyModel attackerModel = attackerRuntime.EnemyModel;
                PlayerStatusUIController targetUIController = playerStatusUIControllers.FirstOrDefault(ui =>
                    ui.GetPlayerRuntime() == targetRuntime
                );
                enemyControllers.TryGetValue(attackerModel, out EnemyController attackerController);
                playerControllers.TryGetValue(targetRuntime, out PlayerController targetController);

                commandQueue.Enqueue(new EnemyAttackCommand(targetRuntime, attackerModel, attackerController, targetController, targetUIController));
            }
        }
    }

    /// <summary>
    /// 防御判定が完了したときに DefenseHandler から呼ばれる
    /// </summary>
    private void ResolveAttack()
    {
        attackHasBeenResolved = true;
    }


    /// <summary>
    /// 敵の行動を順次実行するコルーチン
    /// </summary>
    private IEnumerator ProcessEnemyActions()
    {
        PrepareAttackCommands();

        while (commandQueue.Count > 0)
        {
            var command = commandQueue.Dequeue();
            var attackCmd = command as EnemyAttackCommand;

            if (attackCmd == null)
            {
                yield return new WaitForSeconds(0.3f);
                continue;
            }

            // この攻撃のコンテキストをセット（共通）
            this.currentPlayerTarget = attackCmd.PlayerTarget;
            this.currentAttackCommand = attackCmd;

            int targetPlayerIndex = players.FindIndex(p => p == currentPlayerTarget);

            int hitCount = Mathf.Max(1, attackCmd.HitCount);

            for (int hit = 0; hit < hitCount; hit++)
            {
                // 既にターゲットが死んでたら中断
                if (currentPlayerTarget == null || currentPlayerTarget.CurrentHP <= 0)
                {
                    break;
                }

                this.attackHasBeenResolved = false;

                // マーカー表示（毎ヒット表示したい場合）
                if (targetPlayerIndex != -1)
                {
                    var markerUI = battleManager.MarkerInstance.GetComponent<TargetMarkerUI>();
                    if (markerUI != null)
                    {
                        markerUI.SetKeyNumber(targetPlayerIndex + 1);
                    }

                    entitiesManager.ShowTargetMarkerOnPlayer(battleManager.MarkerInstance, targetPlayerIndex);
                }

                // 連撃の間隔：1発目は従来通り0.5、2発目以降は短め
                yield return new WaitForSeconds(hit == 0 ? 0.5f : 0.2f);

                // 攻撃コマンド実行（アニメ再生＋待機）
                yield return StartCoroutine(attackCmd.Do());

                // マーカー非表示
                entitiesManager.HideTargetMarker(battleManager.MarkerInstance);

                // アニメイベント不発フォールバック（毎ヒット分必要）
                if (!attackHasBeenResolved)
                {
                    Debug.LogWarning($"P{targetPlayerIndex + 1}: アニメーションイベント不発のため強制実行 (hit {hit + 1}/{hitCount})");
                    StartCoroutine(defenseHandler.StartDefenseWindowCoroutine(currentPlayerTarget, ResolveAttack));
                }

                yield return new WaitUntil(() => attackHasBeenResolved == true);
                yield return null;

                // 念のため：勝敗確定してたら終了（任意）
                if (battleManager != null && battleManager.IsBattleEnded) yield break;
            }

            // 次の敵へ
            yield return null;
        }

        yield return new WaitForSeconds(1.0f);
        defenseHandler.DisableDefenseInput();
        TurnFinished?.Invoke();
    }


    /// <summary>
    /// PlayerDefenseHandler からダメージ発生時に呼ばれる
    /// </summary>
    private void HandleDamageToPlayer(PlayerRuntime target)
    {
        if (currentAttackCommand != null)
        {
            currentAttackCommand.ApplyDamageAfterJudgement();
        }
    }

    private void OnDestroy()
    {
        defenseHandler.OnDefenseResultFeedback -= battleManager.ShowDefenseFeedback;
        defenseHandler.OnDamageToPlayer -= HandleDamageToPlayer;
    }
}