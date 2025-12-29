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
    [SerializeField] private float _deadMoveDurationSeconds = 1.0f;
    [SerializeField] private Vector3 _deadTargetPosition = new Vector3(-30f, 0f, 10f);

    public event System.Action OnAttackHitMoment;

    private EnemyModel _model;
    private EnemyStatusUIController _statusUI;
    private Animator _animator;

    private readonly Dictionary<string, float> _clipLengthCache = new Dictionary<string, float>();

    private EnemyHeightAdjuster _heightAdjuster;
    private MeleeMovement _meleeMovement;
    private bool _isDead;

    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
    private static readonly int AttackIdHash = Animator.StringToHash("AttackID");

    private static readonly List<string> AnimatorAttackStateNames = new List<string>
    {
        "Attack1",
        "Attack2",
        "Attack3",
    };

    public void Init(EnemyModel enemyModel, Transform targetPlayer)
    {
        _model = enemyModel;

        if (_model == null)
        {
            Debug.LogError("EnemyController.Init: enemyModel is null.", this);
            return;
        }

        if (_model.CharacterPrefab != null)
        {
            Quaternion desiredLocalRotation = Quaternion.Euler(_model.InitialRotation);
            Quaternion desiredWorldRotation = transform.rotation * desiredLocalRotation;

            GameObject instance = Instantiate(_model.CharacterPrefab, transform.position, desiredWorldRotation, transform);

            // 高さ調整
            if (Mathf.Abs(_model.AttackHeightOffset) > 0.001f)
            {
                _heightAdjuster = instance.GetComponent<EnemyHeightAdjuster>();
                if (_heightAdjuster == null) _heightAdjuster = instance.AddComponent<EnemyHeightAdjuster>();
                _heightAdjuster.Setup(instance.transform.position);
            }

            // 近接移動
            if (_model.AttackType == EnemyAttackType.Melee)
            {
                _meleeMovement = instance.GetComponent<MeleeMovement>();
                if (_meleeMovement == null) _meleeMovement = instance.AddComponent<MeleeMovement>();
            }

            // Animator
            _animator = instance.GetComponent<Animator>();
            if (_animator == null)
            {
                Debug.LogError("EnemyController: CharacterPrefab has no Animator.", instance);
                return;
            }

            // 共通Relay（敵もプレイヤーも同じイベント名）
            AnimationHitRelay relay = instance.GetComponent<AnimationHitRelay>();
            if (relay == null) relay = instance.AddComponent<AnimationHitRelay>();
            relay.OnHit += TriggerAttackHit;
        }

        CacheAnimationLengths();
    }

    /// <summary>
    /// Animator内の全クリップ長をキャッシュする
    /// </summary>
    private void CacheAnimationLengths()
    {
        if (_animator == null || _animator.runtimeAnimatorController == null) return;

        _clipLengthCache.Clear();
        foreach (AnimationClip clip in _animator.runtimeAnimatorController.animationClips)
        {
            if (!_clipLengthCache.ContainsKey(clip.name))
            {
                _clipLengthCache.Add(clip.name, clip.length);
            }
        }
    }

    /// <summary>
    /// ステート名からクリップの長さを取得する
    /// </summary>
    private float GetAnimationLengthByStateName(string stateName)
    {
        if (_clipLengthCache.TryGetValue(stateName, out float length))
        {
            return length;
        }

        string match = _clipLengthCache.Keys.FirstOrDefault(k => k.Contains(stateName));
        if (!string.IsNullOrEmpty(match))
        {
            return _clipLengthCache[match];
        }

        Debug.LogWarning($"EnemyController: clip not found. (State: {stateName})", this);
        return 1.0f;
    }

    /// <summary>
    /// 攻撃アニメーションを再生する
    /// </summary>
    public float PlayRandomAttackAnimation()
    {
        if (_animator == null) return 0.5f;

        int availableCount = AnimatorAttackStateNames.Count;
        int randomAttackId = Random.Range(0, availableCount);

        _animator.SetInteger(AttackIdHash, randomAttackId);
        _animator.SetTrigger(AttackTriggerHash);

        string stateName = AnimatorAttackStateNames[randomAttackId];
        float clipLength = GetAnimationLengthByStateName(stateName);

        if (_heightAdjuster != null)
        {
            _heightAdjuster.ApplyHeightOffset(clipLength, _model.AttackHeightOffset);
        }

        return clipLength;
    }

    /// <summary>
    /// 攻撃エフェクト再生
    /// </summary>
    public void PlayAttackEffect(EnemyAttackType type, Vector3 targetPosition)
    {
        if (_model == null) return;
        if (_model.AttackEffectPrefab == null) return;

        // 共通エフェクト生成（寿命自動）
        Vector3 pos = targetPosition + Vector3.up;
        EffectSpawner.SpawnAndAutoDestroy(_model.AttackEffectPrefab, pos, Quaternion.identity);
    }

    /// <summary>
    /// ターゲットへ移動する（コンポーネントに委譲）
    /// </summary>
    public IEnumerator MoveToTarget(Vector3 targetPosition)
    {
        if (_meleeMovement != null)
        {
            yield return _meleeMovement.MoveToTarget(targetPosition);
        }
    }

    /// <summary>
    /// 元の位置へ戻る（コンポーネントに委譲）
    /// </summary>
    public IEnumerator ReturnToOriginalPosition()
    {
        if (_meleeMovement != null)
        {
            yield return _meleeMovement.ReturnToOriginLocal();
        }
    }

    public void SetStatusUI(EnemyStatusUIController ui)
    {
        _statusUI = ui;
    }

    public void UpdateHealthUI(float currentHp)
    {
        if (_statusUI != null)
        {
            _statusUI.UpdateHP(currentHp);
        }
    }

    public void TriggerAttackHit()
    {
        OnAttackHitMoment?.Invoke();
    }

    /// <summary>
    /// 死亡時の演出：画面外へ平行移動して消える
    /// </summary>
    public IEnumerator DeadSequence()
    {
        Debug.Log($"[Death] DeadSequence called name={gameObject.name} active={gameObject.activeInHierarchy}", this);
        if (_isDead) yield break;
        _isDead = true;
        Debug.Log($"[Death] DeadSequence START name={gameObject.name} active={gameObject.activeInHierarchy}", this);

        Collider collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false;

        Sequence seq = DOTween.Sequence();
        seq.Join(transform.DOMove(_deadTargetPosition, _deadMoveDurationSeconds).SetEase(Ease.InBack));

        SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null)
        {
            seq.Join(sprite.DOFade(0f, _deadMoveDurationSeconds));
        }

        yield return seq.WaitForCompletion();
        gameObject.SetActive(false);
    }
}