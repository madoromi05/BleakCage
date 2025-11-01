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
    private Dictionary<EnemyModel, EnemyController> enemyControllers;
    private Dictionary<PlayerModel, PlayerController> playerControllers;

    // --- ゲージと入力の定数 ---
    private const float GUARD_RECOVERY_SMALL = 10f;       // ガード成功回復
    private const float HIT_RECOVERY_MEDIUM = 20f;        // 被弾回復
    private const float COUNTER_RECOVERY_LARGE = 50f;     // カウンター成功回復
    private const float GUARD_DRAIN_PER_SECOND = 10f;     // ★ 追加: ガード中のゲージ消費量(毎秒)

    private int defenseInput = 0; // 押された防御キー (1, 2, 3) - "押した瞬間" の判定用
    private bool[] isDefending = new bool[3];

    private void Awake()
    {
        damageStrategy = new EnemyAttackDamage();
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
    }

    public void StartEnemyTurn()
    {
        commandQueue.Clear();
        inputReader.OnDefend += HandleDefenseInput;
        inputReader.OnDefendCanceled += HandleDefenseInputCanceled;

        for (int i = 0; i < isDefending.Length; i++)
        {
            isDefending[i] = false;
        }

        StartCoroutine(ProcessEnemyActions());
    }

    /// <summary>
    /// InputReaderから押されたキー(1,2,3)を受け取る
    /// </summary>
    private void HandleDefenseInput(int playerIndex)
    {
        // "押した瞬間" のキーを記録 (カウンター判定用)
        this.defenseInput = playerIndex;

        // "ホールド状態" をONにする
        if (playerIndex > 0 && playerIndex <= 3)
        {
            isDefending[playerIndex - 1] = true; // ★ 追加
        }
    }

    /// <summary>
    /// InputReaderから防御キーが "離された" ことを受け取る
    /// </summary>
    private void HandleDefenseInputCanceled(int playerIndex)
    {
        // "ホールド状態" をOFFにする
        if (playerIndex > 0 && playerIndex <= 3)
        {
            isDefending[playerIndex - 1] = false;
        }
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
                    if (!enemyControllers.TryGetValue(attacker, out EnemyController attackerController))
                    {
                        Debug.LogError($"EnemyModel (ID: {attacker.EnemyID}) に対応する EnemyController が見つかりません。", this);
                        continue;
                    }
                    if (!playerControllers.TryGetValue(target, out PlayerController targetController))
                    {
                        Debug.LogError($"PlayerModel (ID: {target.PlayerID}) に対応する PlayerController が見つかりません。", this);
                        continue;
                    }
                    commandQueue.Enqueue(new EnemyAttackCommand(target, attacker, attackerController, targetController, damageStrategy, targetUIController));
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

        // 2. 防御用の入力マップを有効にする
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

            // --- ★ ここから防御/カウンター処理を大幅に変更 ★ ---

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

            defenseInput = 0; // "Press" event tracker for this attack
            float pressTime = -1f; // "Press" event が発生した時間
            bool isHoldingCorrectKey = false;
            DefenseResult result = DefenseResult.None; // Default to hit

            while (timer < defenseWindow)
            {
                timer += Time.deltaTime;

                // 1. "Press" event check (for timing)
                // HandleDefenseInput が defenseInput をセットする
                if (defenseInput == targetPlayerNum)
                {
                    pressTime = timer; // "押した瞬間" の時間を記録
                    defenseInput = 0; // イベントを消費
                }

                // 2. "Hold" state check
                // HandleDefenseInput/Canceled が isDefending[] を更新する
                isHoldingCorrectKey = isDefending[targetPlayerIndex];

                // 3. Continuous Gauge Drain (ゲージ消費ロジック)
                if (isHoldingCorrectKey)
                {
                    // 毎秒10のペースでゲージを消費
                    float drainAmount = GUARD_DRAIN_PER_SECOND * Time.deltaTime;
                    if (!battleManager.TrySpendGuardGauge(drainAmount))
                    {
                        // ゲージが尽きたら強制的にガード解除
                        isHoldingCorrectKey = false;
                        isDefending[targetPlayerIndex] = false; // "ホールド" 状態を強制解除
                        Debug.Log("ガードゲージが尽きた！");
                    }
                }

                yield return null;
            }

            // TODO: UIの「狙われている！」表示を消す

            // 7. 防御結果を判定 (攻撃が "着弾" する瞬間の判定)

            if (isHoldingCorrectKey) // ウィンドウ終了時にキーを押し続けていたか？
            {
                // 押し続けていた
                if (pressTime >= justWindowStart)
                {
                    // "Just" window 内で押し始めていた -> カウンター
                    result = DefenseResult.Counter;
                    battleManager.IncrementCounterCount();
                    battleManager.AddGuardGauge(COUNTER_RECOVERY_LARGE);
                    Debug.Log($"P{targetPlayerNum}: カウンター成功！ (+{COUNTER_RECOVERY_LARGE})");
                }
                else
                {
                    // "Just" window 以前から押し始めていた (pressTime >= 0 or -1) -> ガード
                    result = DefenseResult.Guard;
                    battleManager.AddGuardGauge(GUARD_RECOVERY_SMALL);
                    Debug.Log($"P{targetPlayerNum}: ガード成功 (+{GUARD_RECOVERY_SMALL})");
                }
            }
            else
            {
                // 攻撃着弾時、キーを押していなかった (or ゲージ切れ) -> 被弾
                result = DefenseResult.None;
                Debug.Log($"P{targetPlayerNum}: 被弾！ (+{HIT_RECOVERY_MEDIUM})");
                battleManager.AddGuardGauge(HIT_RECOVERY_MEDIUM);
            }

            // 8. コマンドに結果をセットして実行
            attackCmd.SetDefenseResult(result);
            yield return StartCoroutine(command.Do());
        }

        // 10. ターン終了処理
        inputReader.EnableBattleActionMap();
        inputReader.OnDefend -= HandleDefenseInput;
        inputReader.OnDefendCanceled -= HandleDefenseInputCanceled;

        TurnFinished?.Invoke();
    }
}