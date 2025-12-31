using System;
using UnityEngine;

/// <summary>
/// プレイヤーのアニメーション再生と、ステート（攻撃・ガード）を制御するクラス
/// </summary>
public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    public event Action OnAttackHitTriggered;

    private AnimatorOverrideController _overrideController;

    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
    private static readonly int IsGuardingHash = Animator.StringToHash("IsGuarding");

    private const string GuardClipName = "Guard";
    private const string AttackClipName = "DummyAttack";

    private bool _isDeathMode = false;

    /// <summary>
    /// アニメーターの初期化と、イベント中継用コンポーネントのセットアップ
    /// </summary>
    /// <param name="animator">対象のアニメーター</param>
    public void Init(Animator animator)
    {
        _animator = animator;
        if (_animator == null) return;
        AnimationHitRelay relay = _animator.gameObject.GetComponent<AnimationHitRelay>();
        if (relay == null)
        {
            relay = _animator.gameObject.AddComponent<AnimationHitRelay>();
        }

        relay.OnHit += HandleAnimationHit;

        // 実行時にアニメーションを差し替えるためのOverrideControllerを作成
        if (_animator.runtimeAnimatorController != null)
        {
            _overrideController = new AnimatorOverrideController(_animator.runtimeAnimatorController);
            _animator.runtimeAnimatorController = _overrideController;
        }
    }

    /// <summary>
    /// 死亡モード（操作不可状態）の有効/無効を切り替えます
    /// </summary>
    public void SetDeathMode(bool isDeathMode)
    {
        _isDeathMode = isDeathMode;
        if (_animator == null) return;

        if (_isDeathMode)
        {
            ResetForDeath();
        }
    }

    /// <summary>
    /// 死亡割り込み対策：再生中の攻撃やガードの残留パラメータを消去します
    /// </summary>
    public void ResetForDeath()
    {
        if (_animator == null) return;

        _animator.ResetTrigger(AttackTriggerHash);
        _animator.SetBool(IsGuardingHash, false);
    }

    /// <summary>
    /// AnimationHitRelayからのイベントを受け取り、外部へ通知します
    /// </summary>
    private void HandleAnimationHit()
    {
        // 死亡中はヒット判定を無視
        if (_isDeathMode) return;

        // 外部（CombatController等）に通知
        OnAttackHitTriggered?.Invoke();
    }

    /// <summary>
    /// 指定されたクリップを攻撃アニメーションとして再生します
    /// </summary>
    /// <param name="clip">再生するAnimationClip</param>
    public void PlayAttackAnimation(AnimationClip clip)
    {
        if (_isDeathMode) return;

        if (clip == null)
        {
            DebugCostom.LogError("PlayAttackAnimation: clip is null.");
            return;
        }
        _overrideController[AttackClipName] = clip;
        _animator.SetTrigger(AttackTriggerHash);
    }

    /// <summary>
    /// ガードアニメーションの再生フラグを制御します
    /// </summary>
    /// <param name="isGuarding">ガード中かどうか</param>
    public void SetGuard(bool isGuarding)
    {
        if (_isDeathMode) return;
        if (_animator == null) return;

        _animator.SetBool(IsGuardingHash, isGuarding);
    }

    /// <summary>
    /// 現在設定されているガードアニメーションの長さを取得します
    /// </summary>
    /// <returns>秒数（設定がない場合はデフォルトの0.5秒）</returns>
    public float GetGuardAnimationLength()
    {
        if (_overrideController != null && _overrideController[GuardClipName] != null)
        {
            return _overrideController[GuardClipName].length;
        }
        return 0.5f;
    }
}