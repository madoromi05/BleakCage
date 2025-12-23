using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI、データをゲームにsetするクラス
///　敵キャラの共通親クラス
/// </summary>
public class EnemyController : MonoBehaviour
{
    [Header("Effects")]
    [SerializeField] private GameObject meleeHitEffectPrefab;   // 近接ヒットエフェクト
    [SerializeField] private GameObject rangedShootEffectPrefab; // 遠距離の発射エフェクト
    [SerializeField] private GameObject rangedHitEffectPrefab;   // 遠距離の着弾エフェクト

    public event System.Action OnAttackHitMoment;
    private EnemyModel model;
    private EnemyStatusUIController statusUI;
    private Animator animator;
    private AnimatorOverrideController overrideController;
    private Vector3 originalPosition;
    private Transform playerTransform;

    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
    private static readonly int AttackIDHash = Animator.StringToHash("AttackID");

    private const string IdleClipName = "Idle";
    private static readonly List<string> AnimatorAttackSlotNames = new List<string>
    {
        //攻撃アニメーションの最大数分作る
        "Attack1",
        "Attack2",
        "Attack3",
    };

    /// <summary>
    /// 敵のデータを初期化し、表示とアニメーションを設定する
    /// </summary>
    public void Init(EnemyModel enemyModel, Transform targetPlayer)
    {
        this.model = enemyModel;
        this.playerTransform = targetPlayer;
        this.originalPosition = transform.position;

        if (model.CharacterPrefab != null)
        {
            Quaternion desiredLocalRotation = Quaternion.Euler(model.InitialRotation);
            Quaternion desiredWorldRotation = this.transform.rotation * desiredLocalRotation;
            // プレハブを生成し、その参照(instance)を保持する
            GameObject instance = Instantiate(model.CharacterPrefab, this.transform.position, desiredWorldRotation, this.transform);
            var receiver = instance.AddComponent<EnemyAnimationReceiver>();
            receiver.Setup(this);
            this.animator = instance.GetComponent<Animator>();
            if (this.animator == null)
            {
                Debug.LogError("生成したキャラクタープレハブに Animator がありません！", instance);
                return;
            }
        }

        overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
        animator.runtimeAnimatorController = overrideController;

        // クリップの上書き (model.EnemyAnimator は "EnemyAnimatorSet" 型)
        if (model.EnemyAnimator != null)
        {
            overrideController[IdleClipName] = model.EnemyAnimator.Idle;
            var clipsFromSet = model.EnemyAnimator.AttackAnimations;
            int clipsToAssign = Mathf.Min(clipsFromSet.Count, AnimatorAttackSlotNames.Count);

            if (clipsFromSet.Count != AnimatorAttackSlotNames.Count)
            {
                Debug.LogWarning($"EnemyAnimatorSet ({model.EnemyAnimator.name}) には {clipsFromSet.Count} 個のアニメが設定されていますが、" +
                                 $"EnemyController の Animator スロットは {AnimatorAttackSlotNames.Count} 個です。" +
                                 $"{clipsToAssign} 個分だけ割り当てます。", this);
            }

            for (int i = 0; i < clipsToAssign; i++)
            {
                if (clipsFromSet[i] != null)
                {
                    overrideController[AnimatorAttackSlotNames[i]] = clipsFromSet[i];
                }
            }
        }
    }

    /// <summary>
    /// OverrideControllerに設定されたクリップの長さを取得する
    /// </summary>
    private float GetAnimationClipLength(string clipNameKey)
    {
        if (overrideController != null && overrideController[clipNameKey] != null)
        {
            return overrideController[clipNameKey].length;
        }
        Debug.LogWarning($"EnemyのOverrideControllerに '{clipNameKey}' のクリップが見つかりません。デフォルトの0.5秒を使用します。", this);
        return 0.5f;
    }

    /// <summary>
    /// ランダムな攻撃アニメーションを再生し、その長さを返す
    /// (EnemyAttackCommand から呼ばれる)
    /// </summary>
    /// <returns>再生したアニメーションの長さ(秒)</returns>
    public float PlayRandomAttackAnimation()
    {
        int availableAttackCount = Mathf.Min(
            model.EnemyAnimator.AttackAnimations.Count,
            AnimatorAttackSlotNames.Count
        );

        int randomAttackID = Random.Range(0, availableAttackCount);

        Debug.Log($"[EnemyController] アニメーション再生: AttackID = {randomAttackID} (利用可能な数: {availableAttackCount})");

        animator.SetInteger(AttackIDHash, randomAttackID);
        animator.SetTrigger(AttackTriggerHash);

        string clipSlotName = AnimatorAttackSlotNames[randomAttackID];
        return GetAnimationClipLength(clipSlotName);
    }

    /// <summary>
    /// 攻撃タイプに応じたエフェクトを再生するメソッド
    /// </summary>
    /// <param name="type">敵の攻撃タイプ (Melee/Ranged)</param>
    /// <param name="targetPosition">攻撃対象（プレイヤー）の位置</param>
    public void PlayAttackEffect(EnemyAttackType type, Vector3 targetPosition)
    {
        switch (type)
        {
            case EnemyAttackType.Melee:
                // 近接攻撃の場合
                if (meleeHitEffectPrefab != null)
                {
                    // 少し位置を上げる(Vector3.up)と足元ではなく体に当たったように見える
                    Instantiate(meleeHitEffectPrefab, targetPosition + Vector3.up, Quaternion.identity);
                }
                break;

            case EnemyAttackType.Ranged:
                // 遠距離攻撃の場合
                // ターゲットの位置に「着弾エフェクト（爆発）」を出す
                if (rangedHitEffectPrefab != null)
                {
                    Instantiate(rangedHitEffectPrefab, targetPosition + Vector3.up, Quaternion.identity);
                }
                break;
        }
    }

    public void SetStatusUI(EnemyStatusUIController ui)
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

    /// <summary>
    /// 攻撃が当たる瞬間のアニメーションイベント
    /// </summary>
    public void TriggerAttackHit()
    {
        // 攻撃が当たる瞬間にイベントを発火させる
        OnAttackHitMoment?.Invoke();
    }
}