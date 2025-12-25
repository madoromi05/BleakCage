using UnityEngine;

/// <summary>
/// モデル（子オブジェクト）にアタッチされ、
/// アニメーションイベントを受け取って親のControllerに通知するクラス
/// </summary>
public class EnemyAnimationReceiver : MonoBehaviour
{
    private EnemyController _parentController;

    /// <summary>
    /// 生成時に親を登録する
    /// </summary>
    public void Setup(EnemyController controller)
    {
        _parentController = controller;
    }

    /// <summary>
    /// Animation Eventから呼ばれるメソッド
    /// </summary>
    public void OnAnimationHit()
    {
        if (_parentController != null)
        {
            _parentController.TriggerAttackHit();
        }
    }
}