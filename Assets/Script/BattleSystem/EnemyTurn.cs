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
    private const float GUARD_DRAIN_PER_SECOND = 10f;     // ガード中のゲージ消費量(毎秒)
    private const float GUARD_COST_ON_SUCCESS = 5f;

    private int defenseInput = 0; // 押された防御キー (1, 2, 3) - "押した瞬間" の判定用
    private bool[] isDefending = new bool[3];
    private bool isJustGuardWindowOpen = false;
    private float justGuardTimer = 0f;
    private const float JUST_GUARD_DURATION = 0.067f;/// 4フレームの秒数 (60FPSの場合: 4 * (1/60) = 約 0.067秒)
    private PlayerModel currentPlayerTarget;
    private EnemyAttackCommand currentAttackCommand;
    private bool attackHasBeenResolved;

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
        foreach (var enemyController in this.enemyControllers.Values)
        {
            if (enemyController != null)
            {
                // EnemyControllerのアニメーションイベント(OnAttackHitMoment)が
                // このスクリプトの StartJustGuardWindow を呼び出すように設定
                enemyController.OnAttackHitMoment += StartJustGuardWindow;
            }
        }
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
            isDefending[playerIndex - 1] = true;
            // 対応するプレイヤーの防御アニメーションを開始する
            if (playerControllers.TryGetValue(players[playerIndex - 1], out PlayerController pc))
            {
                pc.SetGuardAnimation(true);
            }
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
            // 対応するプレイヤーの防御アニメーションを停止する
            if (playerControllers.TryGetValue(players[playerIndex - 1], out PlayerController pc))
            {
                pc.SetGuardAnimation(false);
            }
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
            }
        }
    }

    private void Update()
    {
        // 1. ジャストガードの受付窓が開いている時だけ処理
        if (isJustGuardWindowOpen)
        {
            justGuardTimer -= Time.deltaTime;

            int targetPlayerIndex = players.FindIndex(p => p == currentPlayerTarget);
            int targetPlayerNum = targetPlayerIndex + 1;

            // 2. ジャストガード成功判定 (受付時間内に "押した" か？)
            if (defenseInput == targetPlayerNum)
            {
                Debug.Log($"P{targetPlayerNum}: カウンター成功！");
                battleManager.IncrementCounterCount();
                battleManager.AddGuardGauge(COUNTER_RECOVERY_LARGE);
                battleManager.ShowDefenseFeedback("COUNTER!!", Color.yellow);
                // 成功したので、この攻撃は処理済み
                isJustGuardWindowOpen = false;
                defenseInput = 0; // 入力イベントを消費
                attackHasBeenResolved = true;
            }
            // 3. 猶予時間が過ぎた場合 (ジャストガード失敗)
            else if (justGuardTimer <= 0f)
            {
                isJustGuardWindowOpen = false;

                // 4. 通常ガード判定 (受付終了時に "押していた" か？)
                if (isDefending[targetPlayerIndex] && battleManager.TrySpendGuardGauge(GUARD_COST_ON_SUCCESS))
                {
                    Debug.Log($"P{targetPlayerNum}: ガード成功");
                    battleManager.AddGuardGauge(GUARD_RECOVERY_SMALL);
                    battleManager.ShowDefenseFeedback("GUARD", Color.cyan);
                }
                else
                {
                    // 5. 被弾処理
                    if (!isDefending[targetPlayerIndex]) Debug.Log($"P{targetPlayerNum}: 被弾！");
                    else Debug.Log($"P{targetPlayerNum}: ゲージ不足で被弾！");

                    battleManager.AddGuardGauge(HIT_RECOVERY_MEDIUM);
                    HandleDamageToPlayer(); // ★ ダメージ処理を呼び出す
                    battleManager.ShowDefenseFeedback("HIT", Color.red);
                }
                attackHasBeenResolved = true; // 処理済み
            }
        }

        // 2. ガード維持によるゲージ消費 (常時監視)
        for (int i = 0; i < isDefending.Length; i++)
        {
            if (isDefending[i])
            {
                float drainAmount = GUARD_DRAIN_PER_SECOND * Time.deltaTime;
                if (!battleManager.TrySpendGuardGauge(drainAmount))
                {
                    // ゲージ切れ
                    isDefending[i] = false;
                    Debug.Log($"P{i + 1} ゲージが尽きた！");
                    if (playerControllers.TryGetValue(players[i], out PlayerController pc))
                    {
                        pc.SetGuardAnimation(false);
                    }
                }
            }
        }
    }

    /// <summary>
    /// (EnemyControllerのアニメーションイベントから) 攻撃が当たる瞬間に呼ばれる
    /// </summary>
    private void StartJustGuardWindow()
    {
        // 既に処理済み (カウンター成功済みなど) なら何もしない
        if (attackHasBeenResolved) return;

        // ターゲットが設定されていなければ何もしない
        if (currentPlayerTarget == null) return;

        Debug.Log($"ジャストガード受付開始 (猶予: {JUST_GUARD_DURATION}秒)");
        isJustGuardWindowOpen = true;
        justGuardTimer = JUST_GUARD_DURATION;
        defenseInput = 0; // 古い入力をクリア
    }

    /// <summary>
    /// ガード失敗時に呼ばれるダメージ処理
    /// </summary>
    private void HandleDamageToPlayer()
    {
        if (currentAttackCommand != null && !attackHasBeenResolved)
        {
            Debug.LogWarning($"P{currentPlayerTarget.PlayerID} にダメージ！");
            currentAttackCommand.ApplyDamageAfterJudgement();
        }
    }


    /// <summary>
    /// 敵の行動を順次実行するコルーチン (ロジック大幅変更)
    /// </summary>
    private IEnumerator ProcessEnemyActions()
    {
        PrepareAttackCommands();
        inputReader.EnableDefenseActionMap();

        while (commandQueue.Count > 0)
        {
            var command = commandQueue.Dequeue();
            var attackCmd = command as EnemyAttackCommand;

            if (attackCmd == null)
            {
                yield return new WaitForSeconds(0.3f);
                continue;
            }

            // 1. この攻撃のコンテキストをセット (Updateが参照するため)
            this.currentPlayerTarget = attackCmd.PlayerTarget;
            this.currentAttackCommand = attackCmd;
            this.attackHasBeenResolved = false; // 攻撃を「未処理」に設定

            int targetPlayerIndex = players.FindIndex(p => p == currentPlayerTarget);

            int targetPlayerNum = targetPlayerIndex + 1; // (1, 2, 3)
            Debug.Log($"！ Player {targetPlayerNum} ({currentPlayerTarget.PlayerName}) が狙われている！");
            if (targetPlayerIndex != -1)
            {
                battleManager.ShowTargetMarkerOnPlayer(targetPlayerIndex);
            }
            // ターゲットマーカー表示からアニメーション開始までのタメ
            yield return new WaitForSeconds(0.5f);

            // 4. 攻撃コマンドを実行 (アニメ再生 + 待機)
            yield return StartCoroutine(command.Do());

            // 5. BattleManagerにマーカー非表示を依頼
            battleManager.HideTargetMarker();

            if (!attackHasBeenResolved)
            {
                Debug.LogWarning($"P{targetPlayerNum}: アニメーションイベントが発火しなかったため、被弾処理を実行します。");
                battleManager.AddGuardGauge(HIT_RECOVERY_MEDIUM);
                HandleDamageToPlayer();
                attackHasBeenResolved = true; // 処理済みにする
            }

            // 次の攻撃の前に1フレーム待機
            yield return null;

        }

        // 10. ターン終了処理
        inputReader.EnableBattleActionMap();
        inputReader.OnDefend -= HandleDefenseInput;
        inputReader.OnDefendCanceled -= HandleDefenseInputCanceled;

        TurnFinished?.Invoke();
    }

    private void OnDestroy()
    {
        if (inputReader != null)
        {
            inputReader.OnDefend -= HandleDefenseInput;
            inputReader.OnDefendCanceled -= HandleDefenseInputCanceled;
        }

        if (enemyControllers != null)
        {
            foreach (var enemyController in this.enemyControllers.Values)
            {
                if (enemyController != null)
                {
                    enemyController.OnAttackHitMoment -= StartJustGuardWindow;
                }
            }
        }
    }
}