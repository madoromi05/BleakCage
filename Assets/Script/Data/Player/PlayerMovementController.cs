using UnityEngine;
using DG.Tweening;
using System.Collections;

public class PlayerMovementController : MonoBehaviour
{
    private Vector3 originalPosition;
    [SerializeField] private float moveDuration = 0.3f;
    [SerializeField] private float returnDuration = 0.5f;

    public void Init(Vector3 startPos)
    {
        this.originalPosition = startPos;
    }

    public IEnumerator MoveToTarget(Vector3 targetPos)
    {
        // ターゲットの手前まで移動
        Vector3 dest = targetPos + (transform.position - targetPos).normalized * 2.0f;

        // DOTweenの完了を待つ
        yield return transform.DOMove(dest, moveDuration).SetEase(Ease.OutCubic).WaitForCompletion();
    }

    public IEnumerator ReturnToOriginalPosition()
    {
        yield return transform.DOLocalMove(originalPosition, returnDuration).SetEase(Ease.InOutQuad).WaitForCompletion();
    }
}