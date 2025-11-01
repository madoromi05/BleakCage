using UnityEngine;
using System.Collections;

/// <summary>
/// UI、データをゲームにsetするクラス
/// </summary>
public class PlayerController : MonoBehaviour
{
    private PlayerModel playerModel;
    private PlayerStatusUIController statusUI;
    private Animator animator;
    private AnimatorOverrideController overrideController;

    // アニメーターのパラメータハッシュ
    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
    private static readonly int IsGuardingHash = Animator.StringToHash("IsGuarding");

    // Animatorのステート名（画像のもの）に合わせる ---
    private const string IdleClipName = "Idle";
    private const string GuardClipName = "Guard";
    private const string AttackClipName = "Attack";

    public void Init(PlayerModel model)
    {
        this.playerModel = model;

        // 1. 3Dモデルのプレハブを生成
        if (model.CharacterPrefab != null)
        {
            Quaternion desiredLocalRotation = Quaternion.Euler(model.InitialRotation);
            Quaternion desiredWorldRotation = this.transform.rotation * desiredLocalRotation;

            GameObject instance = Instantiate(model.CharacterPrefab, this.transform.position, desiredWorldRotation, this.transform);

            this.animator = instance.GetComponent<Animator>();
            if (this.animator == null)
            {
                Debug.LogError("生成したキャラクタープレハブに Animator がありません！", instance);
                return;
            }
        }
        else
        {
            Debug.LogError($"PlayerModel.CharacterPrefabが設定されていません！ (PlayerID: {model.PlayerID})", this.gameObject);
            return; // Animatorがないのでここで処理終了
        }

        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError("キャラクターのAnimatorにベースとなるAnimator Controllerが設定されていません！", this.animator.gameObject);
            return;
        }
        overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
        animator.runtimeAnimatorController = overrideController;

        if (model.PlayerAnimator != null)
        {
            overrideController[IdleClipName] = model.PlayerAnimator.Idle;
            overrideController[GuardClipName] = model.PlayerAnimator.Guard;
        }
        else
        {
            Debug.LogError($"PlayerModel (ID: {model.PlayerID}) に PlayerAnimator (ScriptableObject) が設定されていません。", this.gameObject);
        }
    }

    /// <summary>
    /// カードに応じた攻撃アニメーションを再生する
    /// </summary>
    public void PlayAttackAnimation(AnimationClip cardAttackClip)
    {
        if (animator == null)
        {
            Debug.LogError("Animatorがnullです！ Init()が完了していません。");
            return;
        }

        if (cardAttackClip != null)
        {
            Debug.Log($"Playing attack animation: {cardAttackClip.name} for PlayerID: {playerModel.PlayerID}");
            overrideController[AttackClipName] = cardAttackClip;
            animator.SetTrigger(AttackTriggerHash);
        }
    }

    /// <summary>
    /// 防御アニメーションの再生状態を設定する
    /// </summary>
    /// <param name="isGuarding">防御中なら true</param>
    public void SetGuardAnimation(bool isGuarding)
    {
        if (animator != null)
        {
            animator.SetBool(IsGuardingHash, isGuarding);
        }
    }

    /// <summary>
    /// OverrideControllerに設定されたクリップの長さを取得する（汎用）
    /// </summary>
    private float GetAnimationClipLength(string clipNameKey)
    {
        if (overrideController != null && overrideController[clipNameKey] != null)
        {
            return overrideController[clipNameKey].length;
        }

        Debug.LogWarning($"PlayerのOverrideControllerに '{clipNameKey}' のクリップが見つかりません。デフォルトの0.5秒を使用します。", this);
        return 0.5f;
    }

    /// <summary>
    /// ガードアニメーションの長さを取得する
    /// </summary>
    public float GetGuardAnimationLength()
    {
        return GetAnimationClipLength(GuardClipName);
    }

    public void SetStatusUI(PlayerStatusUIController ui)
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