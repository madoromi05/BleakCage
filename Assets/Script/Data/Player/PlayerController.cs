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
    private Transform rightHandSocket;
    private Transform leftHandSocket;
    private float moveDuration = 0.3f;   // 接近にかかる時間
    private float returnDuration = 0.5f; // 戻るにかかる時間

    // アニメーターのパラメータハッシュ
    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
    private static readonly int IsGuardingHash = Animator.StringToHash("IsGuarding");

    // Animatorのステート名（画像のもの）に合わせる ---
    private const string IdleClipName = "Idle";
    private const string GuardClipName = "Guard";
    private const string AttackClipName = "DummyAttack";

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
            CharacterBoneHolder boneHolder = instance.GetComponent<CharacterBoneHolder>();
            if (boneHolder != null)
            {
                this.rightHandSocket = boneHolder.RightHandTransform;
                this.leftHandSocket = boneHolder.LeftHandTransform;
            }
            else
            {
                Debug.LogError($"キャラクタープレハブ {instance.name} に 'CharacterBoneHolder' が付いていません！ 武器を持たせられません。");
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
    /// <param name="cardModel">使用するカードのデータ（ここを修正！）</param>
    /// <param name="targetEnemy">攻撃対象のTransform</param>
    public IEnumerator AttackSequence(CardModel cardModel, Transform targetEnemy)
    {
        AnimationClip cardAttackClip = cardModel.AttackAnimation;
        GameObject currentWeaponInstance = null;

        if (cardModel.WeaponPrefab != null)
        {
            Transform handTransform = (cardModel.WeaponHand == HandPosition.RightHand)
                                      ? this.rightHandSocket
                                      : this.leftHandSocket;

            if (handTransform != null)
            {
                // 武器生成
                currentWeaponInstance = Instantiate(cardModel.WeaponPrefab, handTransform);

                // 名前からクローンを削除してアニメーションに認識されるようにする
                currentWeaponInstance.name = cardModel.WeaponPrefab.name;
                // 攻撃対象との位置合わせ
                currentWeaponInstance.transform.localPosition = Vector3.zero;
                currentWeaponInstance.transform.localRotation = Quaternion.identity;
            }
            else
            {
                Debug.LogWarning($"武器を装備しようとしましたが、{(cardModel.WeaponHand == HandPosition.RightHand ? "右手" : "左手")}のTransformが取得できていません。CharacterBoneHolderの設定を確認してください。");
            }
        }

        if (cardModel.IsMelee)
        {
            Vector3 targetPosition = targetEnemy.position + (transform.position - targetEnemy.position).normalized * 1.5f;

            transform.DOMove(targetPosition, moveDuration).SetEase(Ease.OutCubic);
            yield return new WaitForSeconds(moveDuration);
        }

        if (cardAttackClip != null)
        {
            overrideController[AttackClipName] = cardAttackClip;
            animator.SetTrigger(AttackTriggerHash);
            yield return new WaitForSeconds(cardAttackClip.length);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("[PlayerController] アニメーション待機完了。元の位置に戻ります。");

        if (currentWeaponInstance != null)
        {
            Destroy(currentWeaponInstance);
        }

        if (cardModel.IsMelee)
        {
            transform.DOLocalMove(originalPosition, returnDuration).SetEase(Ease.InOutQuad);
            yield return new WaitForSeconds(returnDuration);
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