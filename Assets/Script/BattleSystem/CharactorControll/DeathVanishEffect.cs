using DG.Tweening;
using UnityEngine;
using System.Collections;

public class DeathVanishEffect : MonoBehaviour
{
    [SerializeField] private float moveDuration = 1.0f;
    [SerializeField] private Vector3 targetPosition;

    public IEnumerator Play()
    {
        var collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false;

        Sequence seq = DOTween.Sequence();
        seq.Join(transform.DOMove(targetPosition, moveDuration).SetEase(Ease.InBack));

        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            if (r.material.HasProperty("_Color"))
                seq.Join(r.material.DOFade(0f, moveDuration));
        }

        yield return seq.WaitForCompletion();
        gameObject.SetActive(false);
    }
}
