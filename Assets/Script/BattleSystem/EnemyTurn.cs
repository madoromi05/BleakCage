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
    [SerializeField] private BattleManager battleManager; // UI表示のため
    [SerializeField] private BattleEntitiesManager entitiesManager;
    [SerializeField] private PlayerDefenseHandler defenseHandler; // 新設

    // --- 元のフィールド ---
    private List<PlayerModel> players;
    private List<EnemyModel> enemies;
    private IEnemyAttackStrategy damageStrategy;
    private Queue<ICommand> commandQueue = new();
    private List<PlayerStatusUIController> playerStatusUIControllers;
    private Dictionary<EnemyModel, EnemyController> enemyControllers;
    private Dictionary<PlayerModel, PlayerController> playerControllers;

    private PlayerModel currentPlayerTarget;
    private EnemyAttackCommand currentAttackCommand;
    private bool attackHasBeenResolved; // 攻撃の解決状態

    private void Awake()
    {
        damageStrategy = new EnemyAttackDamage();
        defenseHandler.OnDefenseResultFeedback += battleManager.ShowDefenseFeedback;
        defenseHandler.OnDamageToPlayer += HandleDamageToPlayer; // ダメージ発生時のコールバックを購読
    }

    public void EnemySetup(List<PlayerModel> players, List<EnemyModel> enemys,
                           Dictionary<EnemyModel, EnemyController> enemyControllers,
                           Dictionary<PlayerModel, PlayerController> playerControllers,
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
                    StartCoroutine(defenseHandler.StartJustGuardWindowCoroutine(currentPlayerTarget, ResolveAttack));
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
    private PlayerModel GetRandomLivingPlayer()
    {
        var livingPlayers = players.Where(p => p != null && p.PlayerHP > 0).ToList();
        if (livingPlayers.Any())
        {
            int choice = Random.Range(0, livingPlayers.Count);
            return livingPlayers[choice];
        }
        return null;
    }

    private void PrepareAttackCommands()
    {
        // (元のロジックは変更なし: コマンドをキューに追加)
        foreach (var attacker in enemies)
        {
            if (attacker == null || attacker.EnemyHP <= 0) continue;
            PlayerModel target = GetRandomLivingPlayer();

            if (target != null)
            {
                int targetIndex = players.FindIndex(p => p == target);
                PlayerStatusUIController targetUIController = playerStatusUIControllers.FirstOrDefault(ui =>
                    ui.GetPlayerRuntime() != null && ui.GetPlayerRuntime().PlayerModel == target
                );

                if (targetUIController == null)
                {
                    Debug.LogError($"ターゲット {target.PlayerName} に一致する PlayerStatusUIController が見つかりません。");
                    continue;
                }

                enemyControllers.TryGetValue(attacker, out EnemyController attackerController);
                playerControllers.TryGetValue(target, out PlayerController targetController);

                commandQueue.Enqueue(new EnemyAttackCommand(target, attacker, attackerController, targetController, damageStrategy, targetUIController));
            }
        }
    }

    /// <summary>
    /// PlayerDefenseHandler からダメージ発生時に呼ばれる
    /// </summary>
    private void HandleDamageToPlayer(PlayerModel target)
    {
        // 被弾処理を EnemyTurn から PlayerDefenseHandler に移動したため、
        // ここでは単にダメージ処理を AttackCommand に委譲する
        if (currentAttackCommand != null)
        {
            Debug.LogWarning($"P{target.PlayerID} にダメージを適用します。");
            currentAttackCommand.ApplyDamageAfterJudgement();
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
            this.attackHasBeenResolved = false; // 攻撃を「未処理」に設定

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
                Debug.LogWarning($"P{targetPlayerIndex + 1}: アニメーションイベントが発火しなかったため、被弾処理を強制実行します。");
                // 強制的に判定コルーチンを開始（時間切れ処理をさせるため）
                StartCoroutine(defenseHandler.StartJustGuardWindowCoroutine(currentPlayerTarget, ResolveAttack));
            }

            yield return new WaitUntil(() => attackHasBeenResolved == true);

            // 次の攻撃の前に1フレーム待機
            yield return null;
        }

        // 10. ターン終了処理
        defenseHandler.DisableDefenseInput();
        TurnFinished?.Invoke();
    }

    private void OnDestroy()
    {
        // (省略: DefenseHandler の DisableDefenseInput で購読解除されているため、ここでは主に敵コントローラーのイベント解除に注力)
        defenseHandler.OnDefenseResultFeedback -= battleManager.ShowDefenseFeedback;
        defenseHandler.OnDamageToPlayer -= HandleDamageToPlayer;
    }
}