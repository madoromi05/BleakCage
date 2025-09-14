using UnityEngine;
using System.Collections;

/// <summary>
/// UI、データをゲームにsetするクラス
/// </summary>
public class EnemyController : MonoBehaviour
{
    EnemyView view;
    private EnemyModel model;
    private Animator animator;
    private AnimatorOverrideController overrideController;

    private const string AttackTriggerName = "AttackTrigger";

    private void Awake()
    {
        view = GetComponent<EnemyView>();
        animator = GetComponent<Animator>();

        // Animator Controllerが設定されているか確認
        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError("AnimatorにベースとなるAnimator Controllerが設定されていません！", this.gameObject);
            return;
        }

        // Override Controllerを初期化
        overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
        animator.runtimeAnimatorController = overrideController;
    }

    /// <summary>
    /// 敵のデータを初期化し、表示とアニメーションを設定する
    /// </summary>
    public void Init(EnemyModel enemyModel)
    {
        this.model = enemyModel;
        view.Show(model);

        if (model.EnemyAnimator != null)
        {
            // Avatarを設定
            if (model.EnemyAnimator.avatar != null)
            {
                animator.avatar = model.EnemyAnimator.avatar;
            }
            else
            {
                Debug.LogWarning($"AnimatorSet「{model.EnemyAnimator.name}」にAvatarが設定されていません。", this.gameObject);
            }

            // 基本的なアニメーションクリップを上書き設定
            overrideController["Idle"] = model.EnemyAnimator.Idle;
            overrideController["Death"] = model.EnemyAnimator.Death;
            overrideController["Damaged"] = model.EnemyAnimator.Damaged;
        }
        else
        {
            Debug.LogError("EnemyModel.EnemyAnimatorが設定されていません！", this.gameObject);
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
            overrideController["DemoAttack"] = cardAttackClip;

            // 2. 攻撃トリガーを引いて、アニメーションを再生
            animator.SetTrigger(AttackTriggerName);
        }
        else
        {
            Debug.LogWarning("再生する攻撃アニメーションクリップがありません。", this.gameObject);
        }
    }

    /// <summary>
    /// ダメージを受けた際のアニメーションを再生する
    /// </summary>
    public void PlayDamagedAnimation()
    {
        animator.SetTrigger("DamagedTrigger");
    }

    /// <summary>
    /// 死亡時のアニメーションを再生する
    /// </summary>
    public void PlayDeathAnimation()
    {
        animator.SetBool("IsDead", true);
    }
}