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

    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
    private static readonly int IsDeadParamHash = Animator.StringToHash("IsDead");

    // アニメーションクリップ名
    private const string IdleClipName = "Idle";
    private const string DeathClipName = "Death";
    private const string DamagedClipName = "Damaged";
    private const string AttackClipName = "DemoAttack";

    private void Awake()
    {
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

        if (model.CharacterPrefab != null)
        {
            Quaternion desiredLocalRotation = Quaternion.Euler(model.InitialRotation);

            // 2. 親(this.transform)のワールド回転と、子のローカル回転を掛け合わせ、
            //    最終的なビジュアルモデルの「ワールド回転」を計算する
            Quaternion desiredWorldRotation = this.transform.rotation * desiredLocalRotation;

            // 3. 計算したワールド回転を使ってビジュアルモデルを生成する
            //    (位置は親と同じ、回転は計算した値、親は this.transform)
            Instantiate(model.CharacterPrefab, this.transform.position, desiredWorldRotation, this.transform);
        }
        else
        {
            Debug.LogError($"PlayerModel.CharacterPrefabが設定されていません！ (PlayerID: {model.PlayerID})", this.gameObject);
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