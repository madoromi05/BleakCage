using UnityEngine;
using System;

public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    public event Action OnAttackHitTriggered;
    private AnimatorOverrideController _overrideController;

    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
    private static readonly int IsGuardingHash = Animator.StringToHash("IsGuarding");

    private const string IdleClipName = "Idle";
    private const string GuardClipName = "Guard";
    private const string AttackClipName = "DummyAttack";

    public void Init(Animator anim)
    {
        _animator = anim;
        var relay = anim.gameObject.GetComponent<AnimationEventRelay>();
        if (relay == null) relay = anim.gameObject.AddComponent<AnimationEventRelay>();
        relay.Setup(this);

        if (anim.runtimeAnimatorController != null)
        {
            _overrideController = new AnimatorOverrideController(anim.runtimeAnimatorController);
            anim.runtimeAnimatorController = _overrideController;
        }
    }

    public void OnAnimationHit() => OnAttackHitTriggered?.Invoke();

    public void PlayAttackAnimation(AnimationClip clip)
    {
        if (clip == null)
        {
            Debug.LogError("再生しようとしたクリップが null です！ 武器データにアニメーションは設定されていますか？");
            return;
        }

        if (_overrideController == null)
        {
            Debug.LogError("OverrideController が null です！ Init は呼ばれていますか？");
            return;
        }

        if (clip != null && _overrideController != null)
        {
            _overrideController[AttackClipName] = clip;
            _animator.SetTrigger(AttackTriggerHash);
        }
    }

    public void SetGuard(bool isGuarding)
    {
        if (_animator != null)
        {
            _animator.SetBool(IsGuardingHash, isGuarding);
        }
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