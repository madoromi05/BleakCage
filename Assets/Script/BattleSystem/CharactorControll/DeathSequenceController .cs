using System.Collections;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 死亡演出共通（任意でAnimatorトリガー→移動退場）
/// </summary>
[DisallowMultipleComponent]
public class DeathSequenceController : MonoBehaviour
{
    [Header("Movement Vanish")]
    [SerializeField] private bool vanishByMove = true;
    [SerializeField] private float deadMoveDurationSeconds = 1.0f;
    [SerializeField] private Vector3 deadTargetPosition = new Vector3(-30f, 0f, 10f);

    [Header("Animator")]
    [SerializeField] private string deadTriggerName = "IsDead";
    [SerializeField] private bool disableAnimatorAfterDead = false;

    private bool _isDead;
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
    }

    public IEnumerator PlayDeathSequence()
    {
        if (_isDead) yield break;
        _isDead = true;

        // Collider OFF
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Animator trigger
        if (_animator != null && !string.IsNullOrEmpty(deadTriggerName))
        {
            _animator.SetTrigger(deadTriggerName);
        }

        // 1 frame wait
        yield return null;

        if (!vanishByMove)
        {
            if (disableAnimatorAfterDead && _animator != null)
            {
                _animator.enabled = false;
            }
            yield break;
        }

        Sequence seq = DOTween.Sequence();
        seq.Join(transform.DOMove(deadTargetPosition, deadMoveDurationSeconds).SetEase(Ease.InBack));

        // Renderer fade（SpriteRenderer/Renderer どちらでも最低限消える）
        SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null)
        {
            seq.Join(sprite.DOFade(0f, deadMoveDurationSeconds));
        }

        yield return seq.WaitForCompletion();
        gameObject.SetActive(false);
    }
}
