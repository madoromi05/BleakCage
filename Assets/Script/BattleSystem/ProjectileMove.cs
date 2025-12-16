using DG.Tweening;
using UnityEngine;
using System;

public class ProjectileMove : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private bool rotateTowardsTarget = true;

    /// <summary>
    /// ターゲットに向かって移動を開始する
    /// </summary>
    public void Fire(Transform target, Action onHitCallback)
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPosition = target.position + Vector3.up * 1.0f;
        if (rotateTowardsTarget)
        {
            transform.LookAt(targetPosition);
        }

        // 到達時間を計算
        float distance = Vector3.Distance(transform.position, targetPosition);
        float duration = distance / speed;

        transform.DOMove(targetPosition, duration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                // 着弾時の処理
                onHitCallback?.Invoke();

                // 必要ならここで着弾エフェクト(ParticleSystem)などをInstantiateする
                Destroy(gameObject);
            });
    }
}
