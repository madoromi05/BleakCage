using System;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private Animator _animator;

    public event Action OnAttackHitTriggered;

    private AnimatorOverrideController _overrideController;

    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
    private static readonly int IsGuardingHash = Animator.StringToHash("IsGuarding");

    private const string GuardClipName = "Guard";
    private const string AttackClipName = "DummyAttack";

    // éÄñSíÜÇÕàÍêÿëÄçÏÇµÇ»Ç¢
    private bool _isDeathMode = false;

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

        if (_animator.runtimeAnimatorController != null)
        {
            _overrideController = new AnimatorOverrideController(_animator.runtimeAnimatorController);
            _animator.runtimeAnimatorController = _overrideController;
        }
    }

    public void SetDeathMode(bool isDeathMode)
    {
        _isDeathMode = isDeathMode;
        if (_animator == null) return;

        if (_isDeathMode)
        {
            ResetForDeath();
        }
    }

    // éÄñSäÑÇËçûÇ›ëŒçÙÅFçUåÇ/ÉKÅ[ÉhÇÃécóØÇè¡Ç∑
    public void ResetForDeath()
    {
        if (_animator == null) return;

        _animator.ResetTrigger(AttackTriggerHash);
        _animator.SetBool(IsGuardingHash, false);
    }

    private void HandleAnimationHit()
    {
        if (_isDeathMode) return;
        OnAttackHitTriggered?.Invoke();
    }

    public void PlayAttackAnimation(AnimationClip clip)
    {
        if (_isDeathMode) return;

        if (clip == null)
        {
            Debug.LogError("PlayAttackAnimation: clip is null.");
            return;
        }

        if (_overrideController == null)
        {
            Debug.LogError("PlayAttackAnimation: OverrideController is null. Did you call Init()?");
            return;
        }

        _overrideController[AttackClipName] = clip;
        _animator.SetTrigger(AttackTriggerHash);
    }

    public void SetGuard(bool isGuarding)
    {
        if (_isDeathMode) return;
        if (_animator == null) return;
        _animator.SetBool(IsGuardingHash, isGuarding);
    }

    public float GetGuardAnimationLength()
    {
        if (_overrideController != null && _overrideController[GuardClipName] != null)
        {
            return _overrideController[GuardClipName].length;
        }
        return 0.5f;
    }
}
