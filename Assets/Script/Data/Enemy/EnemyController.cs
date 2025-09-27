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

    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
    private static readonly int DamagedTriggerHash = Animator.StringToHash("DamagedTrigger");
    private static readonly int IsDeadParamHash = Animator.StringToHash("IsDead");

    // アニメーションクリップ名も定数化しておくと管理が楽になる
    private const string IdleClipName = "Idle";
    private const string DeathClipName = "Death";
    private const string DamagedClipName = "Damaged";
    private const string AttackClipName = "DemoAttack";

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

        if (model.EnemyAvatar != null)
        {
            animator.avatar = model.EnemyAvatar;
        }
        else
        {
            // Avatarが設定されていない場合、警告を出す
            if (model.EnemyAnimator != null)
            {
                Debug.LogWarning($"AnimatorSet「{model.EnemyAnimator.name}」にAvatarが設定されていません。", this.gameObject);
            }
            else
            {
                Debug.LogError("EnemyModel.EnemyAnimatorが設定されていません！", this.gameObject);
            }
        }

        if (model.EnemyAnimator == null)
        {
            Debug.LogError("EnemyModel.EnemyAnimatorが設定されていません！", this.gameObject);
            return;
        }

        // 定数を使ってアニメーションクリップを上書き設定
        overrideController[IdleClipName] = model.EnemyAnimator.Idle;
        overrideController[DeathClipName] = model.EnemyAnimator.Death;
        overrideController[DamagedClipName] = model.EnemyAnimator.Damaged;
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

            // 2. 攻撃トリガーを引いて、アニメーションを再生
            animator.SetTrigger(AttackTriggerHash);
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
        animator.SetTrigger(DamagedTriggerHash);
    }

    /// <summary>
    /// 死亡時のアニメーションを再生する
    /// </summary>
    public void PlayDeathAnimation()
    {
        animator.SetBool(IsDeadParamHash, true);
    }
}