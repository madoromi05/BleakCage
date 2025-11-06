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
    [SerializeField] private BattleInputReader _inputReader;
    [SerializeField] private GuardGaugeSystem _gaugeSystem;
    [SerializeField] private PlayerTurn _playerTurn;

    public event System.Action<string, Color> OnDefenseResultFeedback;
    public event System.Action<PlayerModel> OnDamageToPlayer; // ダメージ発生時に通知

    // --- ゲージと入力の定数 ---
    private const float GUARD_RECOVERY_SMALL = 10f;
    private const float HIT_RECOVERY_MEDIUM = 20f;
    private const float COUNTER_RECOVERY_LARGE = 50f;
    private const float GUARD_DRAIN_PER_SECOND = 10f;
    private const float GUARD_COST_ON_SUCCESS = 5f;

    /// <summary>
    /// ジャストガード(カウンター)になる受付時間(フレーム管理)
    /// </summary>
    private const int JUST_GUARD_DURATION = 1;
    /// <summary>
    /// 通常ガードになる受付時間（この時間までホールドしていればガード)
    /// </summary>
    private const int NORMAL_GUARD_WINDOW_DURATION = 3;


    // --- 状態変数 ---
    private List<PlayerModel> _players;
    private Dictionary<PlayerModel, PlayerController> _playerControllers;
    private int _defenseInput = 0; // "押した瞬間" の判定用
    private bool[] _isDefending;   // "ホールド状態" の判定用
    private bool _isDefenseWindowOpen = false;
    private PlayerModel _currentPlayerTarget;

    private System.Action _currentDefenseResolutionCallback;
    private Coroutine _defenseMonitoringCoroutine;

    public void Init(List<PlayerModel> players, Dictionary<PlayerModel, PlayerController> playerControllers)
    {
        _players = players;
        _playerControllers = playerControllers;
        _isDefending = new bool[players.Count];
    }

    public void EnableDefenseInput()
    {
        Debug.Log("Defense input enabled.");
        _inputReader.EnableDefenseActionMap();
        _inputReader.OnDefend += HandleDefenseInput;
        _inputReader.OnDefendCanceled += HandleDefenseInputCanceled;

        for (int i = 0; i < _isDefending.Length; i++)
        {
            _isDefending[i] = false;
        }

        if (_defenseMonitoringCoroutine != null)
        {
            StopCoroutine(_defenseMonitoringCoroutine);
        }
        _defenseMonitoringCoroutine = StartCoroutine(GuardMonitoringCoroutine());
    }

    public void DisableDefenseInput()
    {
        _inputReader.EnableBattleActionMap();
        _inputReader.OnDefend -= HandleDefenseInput;
        _inputReader.OnDefendCanceled -= HandleDefenseInputCanceled;

        if (_defenseMonitoringCoroutine != null)
        {
            StopCoroutine(_defenseMonitoringCoroutine);
            _defenseMonitoringCoroutine = null;
        }

        for (int i = 0; i < _players.Count; i++)
        {
            if (_playerControllers.TryGetValue(_players[i], out PlayerController pc))
            {
                pc.SetGuardAnimation(false);
            }
        }
    }

    private void HandleDefenseInput(int playerIndex)
    {
        // 押した瞬間の入力として記録
        // defenseInput はジャストガード判定で消費される
        _defenseInput = playerIndex;

        if (playerIndex > 0 && playerIndex <= _players.Count)
        {
            int index = playerIndex - 1;
            _isDefending[index] = true; // ホールド状態をON
            if (_playerControllers.TryGetValue(_players[index], out PlayerController pc))
            {
                pc.SetGuardAnimation(true);
            }
        }
    }

    private void HandleDefenseInputCanceled(int playerIndex)
    {
        if (playerIndex > 0 && playerIndex <= _players.Count)
        {
            int index = playerIndex - 1;
            _isDefending[index] = false; // ホールド状態をOFF
            if (_playerControllers.TryGetValue(_players[index], out PlayerController pc))
            {
                pc.SetGuardAnimation(false);
            }
        }
    }

    // ガードホールド中のゲージ消費コルーチン (変更なし)
    private IEnumerator GuardMonitoringCoroutine()
    {
        yield return new WaitUntil(() => _players != null && _isDefending != null && _gaugeSystem != null);
        while (true)
        {
            for (int i = 0; i < _players.Count; i++)
            {
                if (_isDefending[i])
                {
                    float drainAmount = GUARD_DRAIN_PER_SECOND * Time.deltaTime;
                    if (!_gaugeSystem.TrySpendGuardGauge(drainAmount))
                    {
                        _isDefending[i] = false;
                        Debug.Log($"P{i + 1} ゲージが尽きた！");
                        if (_playerControllers.TryGetValue(_players[i], out PlayerController pc))
                        {
                            pc.SetGuardAnimation(false);
                        }
                    }
                }
            }
            yield return null;
        }
    }

    /// <summary>
    /// ガードまたは被弾の最終判定
    /// (通常ガード受付時間 終了時、またはジャストガード失敗時に呼ばれる)
    /// </summary>
    private void ResolveAttackDamage(int targetPlayerIndex)
    {
        int targetPlayerNum = targetPlayerIndex + 1;

        // "ホールド状態" (isDefending) かどうかだけを見る
        if (_isDefending[targetPlayerIndex] && _gaugeSystem.TrySpendGuardGauge(GUARD_COST_ON_SUCCESS))
        {
            Debug.Log($"P{targetPlayerNum}: ガード成功");
            _gaugeSystem.AddGuardGauge(GUARD_RECOVERY_SMALL);
            OnDefenseResultFeedback?.Invoke("GUARD", Color.cyan);
        }
        else
        {
            if (!_isDefending[targetPlayerIndex]) Debug.Log($"P{targetPlayerNum}: 被弾！ (ガードしていない)");
            else Debug.Log($"P{targetPlayerNum}: ゲージ不足で被弾！");

            _gaugeSystem.AddGuardGauge(HIT_RECOVERY_MEDIUM);
            OnDefenseResultFeedback?.Invoke("HIT", Color.red);
            OnDamageToPlayer?.Invoke(_currentPlayerTarget);
        }

        _currentDefenseResolutionCallback?.Invoke();
        _currentDefenseResolutionCallback = null;
    }


    /// <summary>
    /// (EnemyControllerのアニメーションイベントから) 攻撃が当たる瞬間に呼ばれる
    /// ジャストガード/通常ガードの受付と判定を行うコルーチン
    /// </summary>
    public IEnumerator StartDefenseWindowCoroutine(PlayerModel target, System.Action onResolved)
    {
        if (_isDefenseWindowOpen)
        {
            Debug.LogWarning("防御ウィンドウが既に開いています。新しいリクエストを即時解決します。");
            onResolved?.Invoke();
            yield break;
        }

        Debug.Log($"★防御受付開始 (ジャスト: {JUST_GUARD_DURATION}フレーム, 通常: {NORMAL_GUARD_WINDOW_DURATION}フレーム)");

        _isDefenseWindowOpen = true;

        int justGuardFrameCounter = JUST_GUARD_DURATION;
        int normalGuardFrameCounter = NORMAL_GUARD_WINDOW_DURATION;

        _defenseInput = 0;

        _currentPlayerTarget = target;
        _currentDefenseResolutionCallback = onResolved;

        int targetPlayerIndex = _players.FindIndex(p => p == _currentPlayerTarget);
        if (targetPlayerIndex == -1)
        {
            Debug.LogWarning("防御ターゲットが見つかりません。");
            _isDefenseWindowOpen = false;
            _currentDefenseResolutionCallback?.Invoke();
            _currentDefenseResolutionCallback = null;
            yield break;
        }
        int targetPlayerNum = targetPlayerIndex + 1;

        // ----------------------------------------------------
        // 1. ジャストガード (カウンター) 判定フェーズ
        // ----------------------------------------------------
        while (justGuardFrameCounter > 0)
        {
            justGuardFrameCounter--;
            normalGuardFrameCounter--; // 通常ガードのカウンターも同時に減らす

            if (_defenseInput == targetPlayerNum)
            {
                Debug.Log($"P{targetPlayerNum}: カウンター成功！");
                _gaugeSystem.IncrementCounterCount();
                _gaugeSystem.AddGuardGauge(COUNTER_RECOVERY_LARGE);
                OnDefenseResultFeedback?.Invoke("COUNTER!!", Color.yellow);

                // メンバ変数を直接ラムダでキャプチャせず、ローカル変数にコピーしてから渡す
                System.Action callbackToRun = _currentDefenseResolutionCallback;
                _playerTurn.StartCounterAction(() => callbackToRun?.Invoke());

                _isDefenseWindowOpen = false;
                _defenseInput = 0; // 入力イベントを消費
                _currentDefenseResolutionCallback = null;
                yield break;
            }

            yield return null;
        }

        // ----------------------------------------------------
        // 2. 通常ガード判定フェーズ
        // ----------------------------------------------------
        // ジャストガードの時間が終了した ( "押されなかった" )
        Debug.Log("ジャストガード時間 終了。通常ガード判定へ移行。");

        // 残りの "通常ガード受付時間" が終わるまで、ひたすら待つ
        // (例: 通常3F, ジャスト1F の場合、残り 3-1 = 2F)
        while (normalGuardFrameCounter > 0)
        {
            // "押した" (defenseInput) 入力は、ジャストガード時間外なので無視する
            if (_defenseInput == targetPlayerNum)
            {
                Debug.Log("入力が遅すぎます (ジャスト失敗)");
                _defenseInput = 0; // 遅れた入力を消費
            }

            normalGuardFrameCounter--;
            yield return null;
        }

        // ----------------------------------------------------
        // 3. 最終判定
        // ----------------------------------------------------
        Debug.Log("通常ガード受付 終了。最終判定へ。");
        _isDefenseWindowOpen = false;

        // "ホールド" (isDefending) していたかどうかを ResolveAttackDamage が判定する
        ResolveAttackDamage(targetPlayerIndex);
    }
}