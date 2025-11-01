using UnityEngine;
using System.Collections;

/// <summary>
/// UI、データをゲームにsetするクラス
/// </summary>
public class PlayerController : MonoBehaviour
{
    private PlayerModel playerModel;
    private PlayerStatusUIController statusUI;
    private Animator animator; // ★ 変数だけ残す
    private AnimatorOverrideController overrideController; // ★ 変数だけ残す

    // アニメーターのパラメータハッシュ
    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
    private static readonly int GuardTriggerHash = Animator.StringToHash("GuardTrigger");

    // --- [重要] Animatorのステート名（画像のもの）に合わせる ---
    private const string IdleClipName = "Idle";
    private const string GuardClipName = "guard arter"; // 画像の "guard arter"
    private const string AttackClipName = "attack will eilll3 arter"; // 画像の "attack will eilll3 arter"

    private void Awake()
    {
        // ★ Awake() で Animator を取得するのをやめる
        // animator = GetComponent<Animator>();
    }

    public void Init(PlayerModel model)
    {
        this.playerModel = model;

        // 1. 3Dモデルのプレハブを生成
        if (model.CharacterPrefab != null)
        {
            Quaternion desiredLocalRotation = Quaternion.Euler(model.InitialRotation);
            Quaternion desiredWorldRotation = this.transform.rotation * desiredLocalRotation;

            // --- [★ 修正 1] ---
            // プレハブを生成し、その参照(instance)を保持する
            GameObject instance = Instantiate(model.CharacterPrefab, this.transform.position, desiredWorldRotation, this.transform);

            // --- [★ 修正 2] ---
            // 生成したインスタンスから "Animator" を取得する
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

        // --- [★ 修正 3] ---
        // "animator" が取得できたので、OverrideController の設定を "Init" で行う
        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError("キャラクターのAnimatorにベースとなるAnimator Controllerが設定されていません！", this.animator.gameObject);
            return;
        }
        overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
        animator.runtimeAnimatorController = overrideController;

        // --- [★ 修正 4] ---
        // 3. アニメーションクリップの上書き
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
    /// ガード（またはカウンター）のアニメーションを再生する
    /// </summary>
    public void PlayGuardAnimation()
    {
        if (animator == null)
        {
            Debug.LogError("Animatorがnullです！ Init()が完了していません。");
            return;
        }
        animator.SetTrigger(GuardTriggerHash);
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