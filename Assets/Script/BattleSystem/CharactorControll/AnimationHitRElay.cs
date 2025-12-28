using System;
using UnityEngine;

/// <summary>
/// アニメーションイベントを受けて Action を発火する共通Relay
/// </summary>
public class AnimationHitRelay : MonoBehaviour
{
    public event Action OnHit;

    /// <summary>
    /// Animation Event から呼ばれる
    /// </summary>
    public void OnAnimationHit()
    {
        OnHit?.Invoke();
    }
}
