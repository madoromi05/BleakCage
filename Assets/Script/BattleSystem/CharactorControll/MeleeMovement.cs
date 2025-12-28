using System.Collections;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 近接移動共通（子オブジェクトが動く想定）
/// </summary>
public class MeleeMovement : MonoBehaviour
{
    private const float MoveDurationSeconds = 0.3f;
    private const float StoppingDistance = 1.5f;

    public IEnumerator MoveToTarget(Vector3 targetPos)
    {
        Vector3 direction = (targetPos - transform.position).normalized;
        Vector3 destination = targetPos - (direction * StoppingDistance);
        destination.y = transform.position.y;

        yield return transform.DOMove(destination, MoveDurationSeconds).WaitForCompletion();
    }

    public IEnumerator ReturnToOriginLocal()
    {
        yield return transform.DOLocalMove(Vector3.zero, MoveDurationSeconds).WaitForCompletion();
    }
}
