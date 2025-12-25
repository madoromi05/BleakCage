using UnityEngine;

/// <summary>
/// 汎用Playerの下のキャラプレファブにアタッチして、攻撃アニメーションイベントを中継するためのクラス
/// </summary>
public class AnimationEventRelay : MonoBehaviour
{
    private PlayerAnimationController _animController;
    public void Setup(PlayerAnimationController animController)
    {
        _animController = animController;
    }

    // 敵に攻撃がヒットしたときのイベント
    public void OnAnimationHit()
    {
        if (_animController != null) _animController.OnAnimationHit();
    }
}
