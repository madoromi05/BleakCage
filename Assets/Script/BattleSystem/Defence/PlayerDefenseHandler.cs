using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

/// <summary>
/// 敵の攻撃に対するプレイヤー側の防御ロジック（ガード、カウンター、ゲージ消費）を担う
/// </summary>
public class PlayerDefenseHandler : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private BattleInputReader inputReader;
    [SerializeField] private GuardGaugeSystem gaugeSystem;
    [SerializeField] private PlayerTurn playerTurn; // カウンター実行のために参照

    public event System.Action<string, Color> OnDefenseResultFeedback;
    public event System.Action<PlayerModel> OnDamageToPlayer; // ダメージ発生時に通知

    // --- ゲージと入力の定数 ---
    private const float GUARD_RECOVERY_SMALL = 10f;
    private const float HIT_RECOVERY_MEDIUM = 20f;
    private const float COUNTER_RECOVERY_LARGE = 50f;
    private const float GUARD_DRAIN_PER_SECOND = 10f;
    private const float GUARD_COST_ON_SUCCESS = 5f;
    private const float JUST_GUARD_DURATION = 0.067f;

    // --- 状態変数 ---
    private List<PlayerModel> players;
    private Dictionary<PlayerModel, PlayerController> playerControllers;
    private int defenseInput = 0;
    private bool[] isDefending;                         // ホールド状態の判定用
    private bool isJustGuardWindowOpen = false;
    private float justGuardTimer = 0f;
    private PlayerModel currentPlayerTarget;
    private System.Action onCounterSuccessCallback;
    private Coroutine defenseMonitoringCoroutine;

    public void Init(List<PlayerModel> players, Dictionary<PlayerModel, PlayerController> playerControllers)
    {
        this.players = players;
        this.playerControllers = playerControllers;
        isDefending = new bool[players.Count];
    }

    public void EnableDefenseInput()
    {
        Debug.Log("Defense input enabled.");
        inputReader.EnableDefenseActionMap();
        inputReader.OnDefend += HandleDefenseInput;
        inputReader.OnDefendCanceled += HandleDefenseInputCanceled;
        for (int i = 0; i < isDefending.Length; i++)
        {
            isDefending[i] = false;
        }
        if (defenseMonitoringCoroutine != null)
        {
            StopCoroutine(defenseMonitoringCoroutine);
        }
        defenseMonitoringCoroutine = StartCoroutine(GuardMonitoringCoroutine());
    }

    public void DisableDefenseInput()
    {
        inputReader.EnableBattleActionMap();
        inputReader.OnDefend -= HandleDefenseInput;
        inputReader.OnDefendCanceled -= HandleDefenseInputCanceled;

        if (defenseMonitoringCoroutine != null)
        {
            StopCoroutine(defenseMonitoringCoroutine);
            defenseMonitoringCoroutine = null;
        }
        // 全ての防御アニメーションを停止
        for (int i = 0; i < players.Count; i++)
        {
            if (playerControllers.TryGetValue(players[i], out PlayerController pc))
            {
                pc.SetGuardAnimation(false);
            }
        }
    }

    /// <summary>
    /// InputReaderから押されたキー(1,2,3)を受け取る
    /// </summary>
    private void HandleDefenseInput(int playerIndex)
    {
        this.defenseInput = playerIndex; // 押した瞬間を記録

        if (playerIndex > 0 && playerIndex <= players.Count)
        {
            int index = playerIndex - 1;
            isDefending[index] = true; // ホールド状態をON
            if (playerControllers.TryGetValue(players[index], out PlayerController pc))
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
        if (playerIndex > 0 && playerIndex <= players.Count)
        {
            int index = playerIndex - 1;
            isDefending[index] = false; // ホールド状態をOFF
            if (playerControllers.TryGetValue(players[index], out PlayerController pc))
            {
                pc.SetGuardAnimation(false);
            }
        }
    }

    /// <summary>
    /// 敵ターン中、ガード維持によるゲージ消費を監視し続けるコルーチン
    /// </summary>
    private IEnumerator GuardMonitoringCoroutine()
    {
        // Init() が呼ばれるまで待機 (安全策)
        yield return new WaitUntil(() => players != null && isDefending != null && gaugeSystem != null);

        // 敵ターン中、ずっとループ (DisableDefenseInput() で停止される)
        while (true)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (isDefending[i])
                {
                    float drainAmount = GUARD_DRAIN_PER_SECOND * Time.deltaTime;
                    if (!gaugeSystem.TrySpendGuardGauge(drainAmount))
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
            yield return null; // 1フレーム待機
        }
    }

    /// <summary>
    /// ジャストガード受付時間が終了した後の、通常ガードまたは被弾の判定と処理
    /// </summary>
    private void ResolveAttackDamage(int targetPlayerIndex)
    {
        int targetPlayerNum = targetPlayerIndex + 1;

        // 4. 通常ガード判定 (受付終了時に "押していた" か？)
        if (isDefending[targetPlayerIndex] && gaugeSystem.TrySpendGuardGauge(GUARD_COST_ON_SUCCESS))
        {
            Debug.Log($"P{targetPlayerNum}: ガード成功");
            gaugeSystem.AddGuardGauge(GUARD_RECOVERY_SMALL);
            OnDefenseResultFeedback?.Invoke("GUARD", Color.cyan);
        }
        else
        {
            // 5. 被弾処理
            if (!isDefending[targetPlayerIndex]) Debug.Log($"P{targetPlayerNum}: 被弾！");
            else Debug.Log($"P{targetPlayerNum}: ゲージ不足で被弾！");

            gaugeSystem.AddGuardGauge(HIT_RECOVERY_MEDIUM);
            OnDefenseResultFeedback?.Invoke("HIT", Color.red);
            OnDamageToPlayer?.Invoke(currentPlayerTarget); // ダメージ処理の実行を要求
        }
        // 攻撃の解決完了
        onCounterSuccessCallback?.Invoke(); // 攻撃の結果を EnemyTurn に返す
        onCounterSuccessCallback = null;
    }


    /// <summary>
    /// (EnemyControllerのアニメーションイベントから) 攻撃が当たる瞬間に呼ばれる
    /// ジャストガードの受付と判定を行うコルーチン
    /// </summary>
    public IEnumerator StartJustGuardWindowCoroutine(PlayerModel target, System.Action onResolved)
    {
        // 既に処理済み（カウンター成功済みなど）の場合は、このコールを無視
        // onCounterSuccessCallback は攻撃が解決されると null になる
        if (onCounterSuccessCallback == null)
        {
            onResolved?.Invoke(); // 既に解決済みだが、解決コールバックだけ呼ぶ
            yield break;
        }

        Debug.Log($"ジャストガード受付開始 (猶予: {JUST_GUARD_DURATION}秒)");

        isJustGuardWindowOpen = true;
        justGuardTimer = JUST_GUARD_DURATION;
        defenseInput = 0; // 古い入力をクリア

        this.currentPlayerTarget = target;
        this.onCounterSuccessCallback = onResolved; // 攻撃の解決完了時に呼ぶコールバック

        // 猶予時間が終わるまで、毎フレーム入力を監視
        while (justGuardTimer > 0f)
        {
            justGuardTimer -= Time.deltaTime;

            int targetPlayerIndex = players.FindIndex(p => p == currentPlayerTarget);
            if (targetPlayerIndex == -1) yield break;

            int targetPlayerNum = targetPlayerIndex + 1;

            // 2. ジャストガード成功判定 (受付時間内に "押した" か？)
            if (defenseInput == targetPlayerNum)
            {
                Debug.Log($"P{targetPlayerNum}: カウンター成功！");
                gaugeSystem.IncrementCounterCount();
                gaugeSystem.AddGuardGauge(COUNTER_RECOVERY_LARGE);
                OnDefenseResultFeedback?.Invoke("COUNTER!!", Color.yellow);

                // カウンター成功時のアクションを実行
                playerTurn.StartCounterAction(() => onCounterSuccessCallback?.Invoke());

                isJustGuardWindowOpen = false;
                defenseInput = 0; // 入力イベントを消費
                onCounterSuccessCallback = null; // コールバックを消費（解決済み）
                yield break; // コルーチン終了
            }

            yield return null; // 1フレーム待機
        }

        // 3. 猶予時間が過ぎた場合 (ジャストガード失敗)
        isJustGuardWindowOpen = false;
        int failedTargetPlayerIndex = players.FindIndex(p => p == currentPlayerTarget);
        if (failedTargetPlayerIndex != -1)
        {
            ResolveAttackDamage(failedTargetPlayerIndex);
        }
        else
        {
            // ターゲットが不明なまま時間切れ（フォールバック）
            onCounterSuccessCallback?.Invoke();
            onCounterSuccessCallback = null;
        }
    }
}