using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 敵キャラのコントローラー
/// アニメーション、エフェクト、および単純な移動制御を行う
/// </summary>
public class EnemyController : MonoBehaviour
{
    public event System.Action OnAttackHitMoment;

    private EnemyModel model;
    private EnemyStatusUIController statusUI;
    private Animator animator;
    private Dictionary<string, float> clipLengthCache = new Dictionary<string, float>();
    private Vector3 originalPosition;
    private EnemyHeightAdjuster heightAdjuster;
    private EnemyMeleeMovement meleeMovement;

    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
    private static readonly int AttackIDHash = Animator.StringToHash("AttackID");

    private static readonly List<string> AnimatorAttackStateNames = new List<string>
    {
        "Attack1",
        "Attack2",
        "Attack3",
    };

    /// <summary>
    /// 初期化処理
    /// </summary>
    public void Init(EnemyModel enemyModel, Transform targetPlayer)
    {
        this.model = enemyModel;
        this.originalPosition = transform.position;

        if (model.CharacterPrefab != null)
        {
            // モデル（子オブジェクト）の生成
            Quaternion desiredLocalRotation = Quaternion.Euler(model.InitialRotation);
            Quaternion desiredWorldRotation = this.transform.rotation * desiredLocalRotation;
            GameObject instance = Instantiate(model.CharacterPrefab, this.transform.position, desiredWorldRotation, this.transform);

            // 高さ調整が必要ならコンポーネント追加
            if (Mathf.Abs(model.AttackHeightOffset) > 0.001f)
            {
                this.heightAdjuster = instance.AddComponent<EnemyHeightAdjuster>();
            }

            // アニメーションイベント受信
            var receiver = instance.AddComponent<EnemyAnimationReceiver>();
            receiver.Setup(this);

            if (model.AttackType == EnemyAttackType.Melee)
            {
                this.meleeMovement = instance.AddComponent<EnemyMeleeMovement>();
            }
            this.animator = instance.GetComponent<Animator>();
            if (this.animator == null)
            {
                Debug.LogError("生成したキャラクタープレハブに Animator がありません！", instance);
                return;
            }
        }

        CacheAnimationLengths();
    }

    /// <summary>
    /// Animator内の全クリップ長をキャッシュする
    /// </summary>
    private void CacheAnimationLengths()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;

        clipLengthCache.Clear();
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (!clipLengthCache.ContainsKey(clip.name))
            {
                clipLengthCache.Add(clip.name, clip.length);
            }
        }
    }

    /// <summary>
    /// ステート名からクリップの長さを取得する
    /// </summary>
    private float GetAnimationLengthByStateName(string stateName)
    {
        if (clipLengthCache.TryGetValue(stateName, out float length))
        {
            return length;
        }

        var match = clipLengthCache.Keys.FirstOrDefault(k => k.Contains(stateName));
        if (match != null)
        {
            return clipLengthCache[match];
        }

        Debug.LogWarning($"EnemyController: クリップが見つからないためデフォルト値を使用します (State: {stateName})", this);
        return 1.0f;
    }

    /// <summary>
    /// 攻撃アニメーションを再生する
    /// </summary>
    public float PlayRandomAttackAnimation()
    {
        int availableAttackCount = AnimatorAttackStateNames.Count;
        int randomAttackID = Random.Range(0, availableAttackCount);

        Debug.Log($"[EnemyController] アニメーション再生: AttackID = {randomAttackID}");

        animator.SetInteger(AttackIDHash, randomAttackID);
        animator.SetTrigger(AttackTriggerHash);

        string stateName = AnimatorAttackStateNames[randomAttackID];
        float clipLength = GetAnimationLengthByStateName(stateName);

        // 高さ調整
        if (heightAdjuster != null)
        {
            heightAdjuster.ApplyHeightOffset(clipLength, model.AttackHeightOffset);
        }

        return clipLength;
    }

    /// <summary>
    /// 攻撃エフェクト再生
    /// </summary>
    public void PlayAttackEffect(EnemyAttackType type, Vector3 targetPosition)
    {
        if (model.AttackEffectPrefab != null)
        {
            Instantiate(model.AttackEffectPrefab, targetPosition + Vector3.up, Quaternion.identity);
        }
    }

    /// <summary>
    /// ターゲットへ移動する（コンポーネントに委譲）
    /// </summary>
    public IEnumerator MoveToTarget(Vector3 targetPosition)
    {
        if (meleeMovement != null)
        {
            yield return meleeMovement.MoveToTarget(targetPosition);
        }
    }

    /// <summary>
    /// 元の位置へ戻る（コンポーネントに委譲）
    /// </summary>
    public IEnumerator ReturnToOriginalPosition()
    {
        if (meleeMovement != null)
        {
            yield return meleeMovement.ReturnToPosition();
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

    public void TriggerAttackHit()
    {
        OnAttackHitMoment?.Invoke();
    }
}