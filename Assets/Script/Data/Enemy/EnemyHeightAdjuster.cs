using DG.Tweening;
using UnityEngine;

/// <summary>
/// 攻撃時に一時的にY座標を持ち上げる機能を提供するクラス
/// </summary>
public class EnemyHeightAdjuster : MonoBehaviour
{
    private Tween _currentTween;
    private Vector3 _basePosition;

    /// <summary>
    /// 初期位置を記憶する（EnemyControllerのInit時に呼ぶと安全）
    /// </summary>
    public void Setup(Vector3 basePos)
    {
        _basePosition = basePos;
    }

    /// <summary>
    /// 指定した高さと時間でジャンプ動作を行う
    /// </summary>
    /// <param name="duration">アニメーションの長さ（戻ってくるまでの時間）</param>
    /// <param name="offsetAmount">上昇させる高さ</param>
    public void ApplyHeightOffset(float duration, float offsetAmount)
    {
        // オフセットが無効、または処理中なら何もしない（あるいは上書きするならTweenをKill）
        if (Mathf.Abs(offsetAmount) < 0.001f) return;

        // 前の動きが残っていたらキャンセルして即座に完了させる
        _currentTween?.Kill(true);

        float targetY = _basePosition.y + offsetAmount;

        transform.DOMoveY(targetY, 0.1f).OnComplete(() =>
        {
            float returnDelay = Mathf.Max(0, duration - 0.3f);

            _currentTween = DOVirtual.DelayedCall(returnDelay, () =>
            {
                if (this != null)
                {
                    transform.DOMoveY(_basePosition.y, 0.2f);
                }
            });
        });
    }

    private void OnDestroy()
    {
        _currentTween?.Kill();
    }
}