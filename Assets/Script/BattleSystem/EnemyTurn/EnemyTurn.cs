using Mono.Cecil;
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
                       List<PlayerStatusUIController> playerStatusUIControllers)
    {
        this.players = players;
        this.enemies = enemys;
        this.enemyControllers = enemyControllers;
        this.playerControllers = playerControllers;
        this.playerStatusUIControllers = playerStatusUIControllers;

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
        foreach (var attacker in enemies)
        {
            if (attacker == null || attacker.EnemyHP <= 0) continue;
            PlayerRuntime targetRuntime = GetRandomLivingPlayer();

            if (targetRuntime != null)
            {
                PlayerStatusUIController targetUIController = playerStatusUIControllers.FirstOrDefault(ui =>
                    ui.GetPlayerRuntime() == targetRuntime
                );
                enemyControllers.TryGetValue(attacker, out EnemyController attackerController);
                playerControllers.TryGetValue(targetRuntime, out PlayerController targetController);

                commandQueue.Enqueue(new EnemyAttackCommand(targetRuntime, attacker, attackerController, targetController, targetUIController));
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

            // 1. この攻撃のコンテキストをセット
            this.currentPlayerTarget = attackCmd.PlayerTarget;
            this.currentAttackCommand = attackCmd;
            this.attackHasBeenResolved = false;

            int targetPlayerIndex = players.FindIndex(p => p == currentPlayerTarget);
            if (targetPlayerIndex != -1)
            {
                entitiesManager.ShowTargetMarkerOnPlayer(battleManager.MarkerInstance, targetPlayerIndex);
            }
            yield return new WaitForSeconds(0.5f);

            // 4. 攻撃コマンドを実行 (アニメ再生 + 待機)
            yield return StartCoroutine(command.Do());

            // 5. BattleManagerにマーカー非表示を依頼
            entitiesManager.HideTargetMarker(battleManager.MarkerInstance);

            // 6. アニメーションイベントが発火しなかった場合のフォールバック
            if (!attackHasBeenResolved)
            {
                Debug.LogWarning($"P{targetPlayerIndex + 1}: アニメーションイベント不発のため強制実行");
                StartCoroutine(defenseHandler.StartDefenseWindowCoroutine(currentPlayerTarget, ResolveAttack));
            }

            yield return new WaitUntil(() => attackHasBeenResolved == true);

            // 次の攻撃の前に1フレーム待機
            yield return null;
        }

        // ターン終了処理
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
            Debug.Log($"P{target.PlayerModel.PlayerID} にダメージを適用します。");
            currentAttackCommand.ApplyDamageAfterJudgement();
        }
    }

    private void OnDestroy()
    {
        defenseHandler.OnDefenseResultFeedback -= battleManager.ShowDefenseFeedback;
        defenseHandler.OnDamageToPlayer -= HandleDamageToPlayer;
    }
}