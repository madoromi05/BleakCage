using UnityEngine;
using System.Collections;
using DG.Tweening;

/// <summary>
/// UI、データをゲームにsetするクラス
/// </summary>
public class PlayerController : MonoBehaviour
{
    private PlayerModel playerModel;
    private PlayerStatusUIController statusUI;
    private Animator animator;
    private AnimatorOverrideController overrideController;
    private Vector3 originalPosition;
    float moveDuration = 0.3f;   // 接近にかかる時間
    float returnDuration = 0.5f; // 戻るにかかる時間

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
        this.originalPosition = transform.localPosition;

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
    /// 敵に接近し、攻撃アニメーションを再生して、元の位置に戻る
    /// </summary>
    /// <param name="cardAttackClip">再生する攻撃アニメ</param>
    /// <param name="targetEnemy">攻撃対象のTransform</param>
    public IEnumerator AttackSequence(AnimationClip cardAttackClip, Transform targetEnemy)
    {
        Debug.Log($"[PlayerController] AttackSequence() 実行開始。再生するクリップ: {cardAttackClip.name}");

        if (animator == null)
        {
            Debug.LogError("Animatorがnullです！ Init()が完了していません。");
            yield break;
        }
        if (cardAttackClip == null)
        {
            Debug.LogError("AttackClipがnullです！");
            yield break;
        }
        if (targetEnemy == null)
        {
            Debug.LogError("targetEnemyがnullです！");
            yield break;
        }

        // 相手の 1.5 ユニット手前の位置を計算
        Vector3 targetPosition = targetEnemy.position + (transform.position - targetEnemy.position).normalized * 1.5f;

        transform.DOMove(targetPosition, moveDuration).SetEase(Ease.OutCubic);
        yield return new WaitForSeconds(moveDuration);

        // 攻撃アニメーション再生
        overrideController[AttackClipName] = cardAttackClip;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);

        animator.SetTrigger(AttackTriggerHash);

        // 5c. アニメーションの長さだけ待機
        yield return new WaitForSeconds(cardAttackClip.length);
        Debug.Log("[PlayerController] アニメーション待機完了。元の位置に戻ります。");

        // 元の位置に戻る
        transform.DOLocalMove(originalPosition, returnDuration).SetEase(Ease.InOutQuad);
        yield return new WaitForSeconds(returnDuration);
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