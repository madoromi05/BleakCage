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
    public event System.Action<PlayerRuntime> OnDamageToPlayer; // ダメージ発生時に通知

    // --- ゲージと入力の定数 ---
    private const float GUARD_RECOVERY_SMALL = 10f;
    private const float HIT_RECOVERY_MEDIUM = 20f;
    private const float COUNTER_RECOVERY_LARGE = 50f;
    private const float GUARD_DRAIN_PER_SECOND = 10f;
    private const float GUARD_COST_ON_SUCCESS = 5f;

    private const int JUST_GUARD_DURATION = 1;
    private const int NORMAL_GUARD_WINDOW_DURATION = 3;

    // --- 状態変数 ---
    private List<PlayerRuntime> _players;
    private Dictionary<PlayerRuntime, PlayerController> _playerControllers;
    private int _defenseInput = 0;
    private int _defenseInputCanceled = 0;
    private bool[] _isDefending;
    private bool _isDefenseWindowOpen = false;
    private PlayerRuntime _currentPlayerTarget;

    private System.Action _currentDefenseResolutionCallback;
    private Coroutine _defenseMonitoringCoroutine;

    //　防御ウィンドウ内の状態を追跡する変数
    private bool _pressedDuringJustWindow = false;
    private bool _pressedDuringNormalWindow = false;
    private bool _counterSuccess = false;

    public void Init(List<PlayerRuntime> players, Dictionary<PlayerRuntime, PlayerController> playerControllers)
    {
        _players = players;
        _playerControllers = playerControllers;
        _isDefending = new bool[players.Count];
    }

    public void EnableDefenseInput()
    {
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

    /// <summary>
    /// 防御入力検知
    /// </summary>
    /// <param name="playerIndex"></param>
    private void HandleDefenseInput(int playerIndex)
    {
        _defenseInput = playerIndex;
        if (playerIndex > 0 && playerIndex <= _players.Count)
        {
            int index = playerIndex - 1;
            _isDefending[index] = true;
            if (_playerControllers.TryGetValue(_players[index], out PlayerController pc))
            {
                pc.SetGuardAnimation(true);
            }
        }
    }

    /// <summary>
    /// 防御入力を離した瞬間を検知
    /// </summary>
    private void HandleDefenseInputCanceled(int playerIndex)
    {
        _defenseInputCanceled = playerIndex;
        if (playerIndex > 0 && playerIndex <= _players.Count)
        {
            int index = playerIndex - 1;
            _isDefending[index] = false;
            if (_playerControllers.TryGetValue(_players[index], out PlayerController pc))
            {
                pc.SetGuardAnimation(false);
            }
        }
    }

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
                        DebugCostom.Log($"P{i + 1} ゲージが尽きた！");
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
    /// (EnemyControllerのアニメーションイベントから) 攻撃が当たる瞬間に呼ばれる
    /// ジャストガード/通常ガードの受付と判定を行うコルーチン
    /// </summary>
    public IEnumerator StartDefenseWindowCoroutine(PlayerRuntime target, System.Action onResolved)
    {
        if (_isDefenseWindowOpen)
        {
            DebugCostom.LogWarning("防御ウィンドウが既に開いています。新しいリクエストを即時解決します。");
            onResolved?.Invoke();
            yield break;
        }

        _isDefenseWindowOpen = true;

        // --- ウィンドウ内状態のリセット ---
        _pressedDuringJustWindow = false;
        _pressedDuringNormalWindow = false;
        _counterSuccess = false;
        _defenseInput = 0;
        _defenseInputCanceled = 0;

        _currentPlayerTarget = target;
        _currentDefenseResolutionCallback = onResolved;

        int targetPlayerIndex = _players.FindIndex(p => p == _currentPlayerTarget);
        if (targetPlayerIndex == -1)
        {
            DebugCostom.LogWarning("防御ターゲットが見つかりません。");
            _isDefenseWindowOpen = false;
            _currentDefenseResolutionCallback?.Invoke();
            _currentDefenseResolutionCallback = null;
            yield break;
        }
        int targetPlayerNum = targetPlayerIndex + 1;

        int justGuardFrameCounter = JUST_GUARD_DURATION;
        int normalGuardFrameCounter = NORMAL_GUARD_WINDOW_DURATION;

        // ジャストガード (カウンター) 判定フェーズ
        while (justGuardFrameCounter > 0)
        {
            justGuardFrameCounter--;
            normalGuardFrameCounter--; // 通常ガードのカウンターも同時に減らす

            HandleJustGuardInputs(targetPlayerNum);

            if (_counterSuccess)
            {
                TriggerCounter(targetPlayerNum);
                yield break; // カウンター成功、コルーチン終了
            }
            yield return null;
        }

        // 通常ガード判定フェーズ
        while (normalGuardFrameCounter > 0)
        {
            normalGuardFrameCounter--;
            HandleNormalGuardInputs(targetPlayerNum);
            yield return null;
        }

        // 最終判定
        _isDefenseWindowOpen = false;

        ResolveFinalDefense(targetPlayerIndex);

        _currentDefenseResolutionCallback?.Invoke();
        _currentDefenseResolutionCallback = null;
    }

    /// <summary>
    /// (Phase 1) ジャストガード時間中の入力を処理する
    /// </summary>
    private void HandleJustGuardInputs(int targetPlayerNum)
    {
        // "押した" 瞬間を検知
        if (_defenseInput == targetPlayerNum)
        {
            _pressedDuringJustWindow = true;
            _pressedDuringNormalWindow = true; // 通常ガードの条件も満たす
            _defenseInput = 0;
        }

        // "離した" 瞬間を検知
        if (_defenseInputCanceled == targetPlayerNum)
        {
            _defenseInputCanceled = 0;

            // ジャスト時間内に「押して」かつ「離した」か？
            if (_pressedDuringJustWindow)
            {
                _counterSuccess = true; // カウンター成功のフラグを立てる
            }
        }
    }

    /// <summary>
    /// (Phase 2) 通常ガード時間中の入力を処理する
    /// </summary>
    private void HandleNormalGuardInputs(int targetPlayerNum)
    {
        if (_defenseInput == targetPlayerNum)
        {
            _pressedDuringNormalWindow = true;
            _defenseInput = 0;
        }

        // "離した" 瞬間を検知 (このフェーズでは特に何もしないが、入力は消費する)
        if (_defenseInputCanceled == targetPlayerNum)
        {
            _defenseInputCanceled = 0;
        }
    }

    /// <summary>
    /// (Phase 1 Result) カウンター成功時の処理
    /// </summary>
    private void TriggerCounter(int targetPlayerNum)
    {
        DebugCostom.Log($"P{targetPlayerNum}: カウンター成功！ (ジャスト時間内に押して離した)");
        _gaugeSystem.IncrementCounterCount();
        _gaugeSystem.AddGuardGauge(COUNTER_RECOVERY_LARGE);
        OnDefenseResultFeedback?.Invoke("COUNTER!!", Color.yellow);

        _playerTurn.StartCounterAction(() => _currentDefenseResolutionCallback?.Invoke());

        _isDefenseWindowOpen = false;
        _currentDefenseResolutionCallback = null;
    }

    /// <summary>
    /// (Phase 3 Result) 最終的なガード/ヒット判定
    /// </summary>
    private void ResolveFinalDefense(int targetPlayerIndex)
    {
        bool isGuardOverlapping = _pressedDuringNormalWindow || _isDefending[targetPlayerIndex];

        if (isGuardOverlapping)
        {
            // ガード入力はあった。ゲージを消費できるか？
            if (_gaugeSystem.TrySpendGuardGauge(GUARD_COST_ON_SUCCESS))
            {
                _gaugeSystem.AddGuardGauge(GUARD_RECOVERY_SMALL);
                OnDefenseResultFeedback?.Invoke("GUARD", Color.cyan);
            }
            else
            {
                TriggerHit(targetPlayerIndex);
            }
        }
        else
        {
            TriggerHit(targetPlayerIndex);
        }
    }

    /// <summary>
    /// (Phase 3 Result) 被弾時の共通処理
    /// </summary>
    private void TriggerHit(int targetPlayerIndex)
    {
        _gaugeSystem.AddGuardGauge(HIT_RECOVERY_MEDIUM);
        OnDefenseResultFeedback?.Invoke("HIT", Color.red);
        OnDamageToPlayer?.Invoke(_players[targetPlayerIndex]);
    }
}