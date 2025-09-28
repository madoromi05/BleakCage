using UnityEngine;
using System.Collections;

/// <summary>
/// UI、データをゲームにsetするクラス
/// </summary>
public class PlayerController : MonoBehaviour
{
    PlayerView view;
    private PlayerModel playerModel;
    private StatusUIController statusUI;
    private Animator animator;
    private AnimatorOverrideController overrideController;

    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
    private static readonly int IsDeadParamHash = Animator.StringToHash("IsDead");

    // アニメーションクリップ名
    private const string IdleClipName = "Idle";
    private const string DeathClipName = "Death";
    private const string DamagedClipName = "Damaged";
    private const string AttackClipName = "DemoAttack";

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

        if (model.PlayerAnimator == null)
        {
            Debug.LogError("PlayerModel.PlayerAnimatorが設定されていません！", this.gameObject);
            return;
        }

        // Avatarのセット
        if (model.PlayerAnimator.avatar != null)
        {
            animator.avatar = model.PlayerAnimator.avatar;
        }
        else
        {
            Debug.LogWarning($"AnimatorSet「{model.PlayerAnimator.name}」にAvatarが設定されていません。", this.gameObject);
        }

        // アニメーションクリップの上書き
        overrideController[IdleClipName] = model.PlayerAnimator.Idle;
        overrideController[DeathClipName] = model.PlayerAnimator.Death;
        overrideController[DamagedClipName] = model.PlayerAnimator.Damaged;
    }

    /// <summary>
    /// カードに応じた攻撃アニメーションを再生する
    /// </summary>
    public void PlayAttackAnimation(AnimationClip cardAttackClip)
    {
        if (cardAttackClip != null)
        {
            // 1. 攻撃アニメーションを、カード固有のものに上書き
            overrideController[AttackClipName] = cardAttackClip;

            // 2. 攻撃トリガーをハッシュ値で引いて、アニメーションを再生
            animator.SetTrigger(AttackTriggerHash);
        }
    }

    public void PlayDeathAnimation()
    {
        // パラメータをハッシュ値で操作する
        animator.SetBool(IsDeadParamHash, true);
    }

    public void SetStatusUI(StatusUIController ui)
    {
        this.statusUI = ui;
    }

    public void UpdateHealthUI(float currentHP)
    {
        if (statusUI != null)
        {
            statusUI.UpdateHP(currentHP);
        }
    }
}