using UnityEngine;
using System;

public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    public event Action OnAttackHitTriggered; // 攻撃判定イベント

    private AnimatorOverrideController overrideController;
    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
    private static readonly int IsGuardingHash = Animator.StringToHash("IsGuarding");

    private const string IdleClipName = "Idle";
    private const string GuardClipName = "Guard";
    private const string AttackClipName = "DummyAttack";

    public void Init(Animator anim, AnimatorSet animSet)
    {
        this.animator = anim;

        // Relayのセットアップ (AnimationEventを受け取るため)
        var relay = anim.gameObject.GetComponent<AnimationEventRelay>();
        if (relay == null) relay = anim.gameObject.AddComponent<AnimationEventRelay>();
        relay.Setup(this);

        // OverrideController設定
        if (anim.runtimeAnimatorController != null)
        {
            overrideController = new AnimatorOverrideController(anim.runtimeAnimatorController);
            anim.runtimeAnimatorController = overrideController;

            if (animSet != null)
            {
                overrideController[IdleClipName] = animSet.Idle;
                overrideController[GuardClipName] = animSet.Guard;
            }
        }
    }

    public void OnAnimationHit() => OnAttackHitTriggered?.Invoke();

    public void PlayAttackAnimation(AnimationClip clip)
    {
        if (clip != null && overrideController != null)
        {
            overrideController[AttackClipName] = clip;
            animator.SetTrigger(AttackTriggerHash);
        }
    }

    public void SetGuard(bool isGuarding)
    {
        if (animator != null)
        {
            animator.SetBool(IsGuardingHash, isGuarding);
        }
    }

    public float GetGuardAnimationLength()
    {
        if (overrideController != null && overrideController[GuardClipName] != null)
        {
            return overrideController[GuardClipName].length;
        }
        return 0.5f; // デフォルト値
    }
}