using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 敵ターンの処理 (カウンター/ガードロジックを含む)
///</summary>
public class EnemyTurn : MonoBehaviour
{
    public event System.Action TurnFinished;

    [Header("Component References")]
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private BattleInputReader inputReader;

    private List<PlayerModel> players;
    private List<EnemyModel> enemies;
    private IEnemyAttackStrategy damageStrategy;
    private Queue<ICommand> commandQueue = new();
    private List<PlayerStatusUIController> playerStatusUIControllers;

    // --- ゲージと入力の定数 ---
    private const float GUARD_RECOVERY_SMALL = 10f;         //ガード成功回復
    private const float HIT_RECOVERY_MEDIUM = 20f;          //被弾回復
    private const float COUNTER_RECOVERY_LARGE = 50f;       //カウンター成功回復

    private int defenseInput = 0; // 押された防御キー (1, 2, 3)

    private void Awake()
    {
        damageStrategy = new EnemyAttackDamage();
    }

    public void EnemySetup(List<PlayerModel> players, List<EnemyModel> enemys,
                         List<PlayerStatusUIController> playerStatusUIControllers)
    {
        this.players = players;
        this.enemies = enemys;
        this.playerStatusUIControllers = playerStatusUIControllers;
    }

    public void StartEnemyTurn()
    {
        commandQueue.Clear();
        inputReader.OnDefend += HandleDefenseInput;
        StartCoroutine(ProcessEnemyActions());
    }

    /// <summary>
    /// InputReaderから押されたキー(1,2,3)を受け取る
    /// </summary>
    private void HandleDefenseInput(int playerIndex)
    {
        this.defenseInput = playerIndex;
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

    /// <summary>
    /// 敵の攻撃コマンドを準備する (変更なし)
    /// </summary>
    private void PrepareAttackCommands()
    {
        foreach (var attacker in enemies)
        {
            if (attacker == null || attacker.EnemyHP <= 0) continue;
            PlayerModel target = GetRandomLivingPlayer();

            if (target != null)
            {
                int targetIndex = players.FindIndex(p => p == target);
                if (targetIndex != -1)
                {
                    PlayerStatusUIController targetUIController = playerStatusUIControllers[targetIndex];
                    commandQueue.Enqueue(new EnemyAttackCommand(target, attacker, damageStrategy, targetUIController));
                }
                else
                {
                    Debug.LogError("ターゲットプレイヤーに対応するUIコントローラーが見つかりませんでした。");
                }
            }
            else
            {
                Debug.LogWarning("攻撃対象となる生存プレイヤーがいません。");
                break;
            }
        }
    }

    /// <summary>
    /// 敵の行動を順次実行するコルーチン
    /// </summary>
    private IEnumerator ProcessEnemyActions()
    {
        // 1. 実行する攻撃コマンドを準備
        PrepareAttackCommands();

        // 2. 防御用の入力マップを有効にする (InputReader側の実装が必要)
        inputReader.EnableDefenseActionMap();

        // 3. キューにたまったコマンドを順に実行
        while (commandQueue.Count > 0)
        {
            var command = commandQueue.Dequeue();
            var attackCmd = command as EnemyAttackCommand;

            // 攻撃コマンド以外は即時実行
            if (attackCmd == null)
            {
                command.Do();
                yield return new WaitForSeconds(0.3f);
                continue;
            }

            // --- ここからが攻撃コマンドの防御/カウンター処理 ---

            // 4. ターゲット情報を取得
            PlayerModel target = attackCmd.PlayerTarget;
            int targetPlayerIndex = players.FindIndex(p => p == target); // (0, 1, 2)
            int targetPlayerNum = targetPlayerIndex + 1; // (1, 2, 3)

            // 5. 防御ウィンドウ (UI表示など)
            Debug.Log($"！ Player {targetPlayerNum} ({target.PlayerName}) が狙われている！");
            // TODO: UIに「Player {targetPlayerNum} が狙われている！」と表示

            // 6. 防御入力の受付
            float defenseWindow = 1.0f; // 1.0秒の受付時間
            float justWindowStart = 0.7f; // 0.7秒～1.0秒がジャスト
            float timer = 0f;
            bool inputReceived = false;
            defenseInput = 0; // 入力をリセット

            while (timer < defenseWindow)
            {
                timer += Time.deltaTime;
                if (defenseInput == targetPlayerNum)
                {
                    inputReceived = true;
                    break; // 正しいキーが押された
                }
                yield return null;
            }

            // TODO: UIの「狙われている！」表示を消す

            // 7. 防御結果を判定
            DefenseResult result = DefenseResult.None;
            if (inputReceived)
            {
                // BattleManagerの共有ゲージを使おうと試みる
                if (battleManager.TrySpendGuardGauge(BattleManager.GUARD_COST))
                {
                    // ゲージ消費成功
                    if (timer >= justWindowStart)
                    {
                        result = DefenseResult.Counter;
                        battleManager.IncrementCounterCount(); // カウンターカウント+1
                        battleManager.AddGuardGauge(COUNTER_RECOVERY_LARGE);
                    }
                    else
                    {
                        result = DefenseResult.Guard;
                        battleManager.AddGuardGauge(GUARD_RECOVERY_SMALL);
                    }
                }
                else
                {
                    // ゲージ不足で防御失敗
                    Debug.Log("ガードゲージ不足！ 防御失敗！");
                    result = DefenseResult.None;
                }
            }

            // 8. 被弾した場合 (入力なし or ゲージ不足)
            if (result == DefenseResult.None)
            {
                battleManager.AddGuardGauge(HIT_RECOVERY_MEDIUM);
            }

            // 9. コマンドに結果をセットして実行
            attackCmd.SetDefenseResult(result);
            command.Do(); // これで改造したDo()が実行される

            yield return new WaitForSeconds(0.5f); // 攻撃ごとのウェイト
        }

        // 10. ターン終了処理
        inputReader.EnableBattleActionMap();
        inputReader.OnDefend -= HandleDefenseInput;

        TurnFinished?.Invoke();
    }
}