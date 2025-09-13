using UnityEngine;
using System.Collections;

/// <summary>
/// UI、データをゲームにsetするクラス
/// </summary>
public class PlayerController : MonoBehaviour
{
    PlayerView view;
    private PlayerModel playerModel;
    private Animator animator;
    private AnimatorOverrideController overrideController;

    private const string AttackTriggerName = "AttackTrigger";

    private void Awake()
    {
        view = GetComponent<PlayerView>();
        animator = GetComponent<Animator>();

        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError("AnimatorにベースとなるAnimator Controllerが設定されていません！", this.gameObject);
            return;
        }

        overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
        animator.runtimeAnimatorController = overrideController;
    }

    public void Init(PlayerModel model)
    {
        this.playerModel = model;
        view.Show(model);

        if (model.PlayerAnimator != null)
        {
            // 封筒があると分かったので、安全に中身(avatar)を確認できる
            if (model.PlayerAnimator.avatar != null)
            {
                animator.avatar = model.PlayerAnimator.avatar;
            }
            else
            {
                Debug.LogWarning($"AnimatorSet「{model.PlayerAnimator.name}」にAvatarが設定されていません。", this.gameObject);
            }

            overrideController["Idle"] = model.PlayerAnimator.Idle;
        }
        else
        {
            // そもそも封筒がなかった場合の処理
            Debug.LogError("PlayerModel.PlayerAnimatorが設定されていません！", this.gameObject);
        }

        if (model.PlayerAnimator != null)
        {
            animator.avatar = model.PlayerAnimator.avatar;
        }
        else
        {
            Debug.LogWarning($"AnimatorSet「{model.PlayerAnimator.name}」にAvatarが設定されていません。", this.gameObject);
        }

        if (model.PlayerAnimator.avatar != null)
        {
            animator.avatar = model.PlayerAnimator.avatar;
        }

        if (model.PlayerAnimator != null)
        {
            overrideController["Idle"] = model.PlayerAnimator.Idle;
            overrideController["Death"] = model.PlayerAnimator.Death;
            overrideController["Damaged"] = model.PlayerAnimator.Damaged;
        }
        else
        {
            Debug.LogError("PlayerModel.PlayerAnimatorが設定されていません！", this.gameObject);
        }
    }

    /// <summary>
    /// カードに応じた攻撃アニメーションを再生する
    /// </summary>
    public void PlayAttackAnimation(AnimationClip cardAttackClip)
    {
        if (cardAttackClip != null)
        {
            // 1. 攻撃アニメーションを、カード固有のものに上書き
            overrideController["Dummy_Attack"] = cardAttackClip;

            // 2. 攻撃トリガーを引いて、アニメーションを再生
            animator.SetTrigger(AttackTriggerName);
        }
    }

    // （PlayDeathAnimationなどのメソッドは、IsDeadパラメーターを操作する形で残します）
    public void PlayDeathAnimation()
    {
        animator.SetBool("IsDead", true);
    }
}