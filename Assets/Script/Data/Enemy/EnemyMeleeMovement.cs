using System.Collections;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 近接タイプ：子オブジェクト自体が移動を担当するクラス
/// 親（Controller）はその場に残り、このオブジェクトだけが動く
/// </summary>
public class EnemyMeleeMovement : MonoBehaviour
{
    private const float MoveDuration = 0.3f;        // 移動時間
    private const float StoppingDistance = 1.5f;    // 止まる距離

    /// <summary>
    /// ターゲット（プレイヤー）の手前まで移動する
    /// </summary>
    public IEnumerator MoveToTarget(Vector3 targetPos)
    {
        Vector3 direction = (targetPos - transform.position).normalized;
        Vector3 destination = targetPos - (direction * StoppingDistance);
        destination.y = transform.position.y;
        yield return transform.DOMove(destination, MoveDuration).WaitForCompletion();
    }

    /// <summary>
    /// 元の位置（親の中心）に戻る
    /// </summary>
    public IEnumerator ReturnToPosition()
    {
        yield return transform.DOLocalMove(Vector3.zero, MoveDuration).WaitForCompletion();
    }
}