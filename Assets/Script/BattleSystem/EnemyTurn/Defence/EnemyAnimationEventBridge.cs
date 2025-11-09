using UnityEngine;

/// <summary>
/// 子オブジェクトのアニメーションイベントを、
/// 親オブジェクトの EnemyController に中継（ブリッジ）するためのスクリプト
/// </summary>
public class EnemyAnimationEventBridge : MonoBehaviour
{
    private EnemyController _parentController;

    private void Awake()
    {
        // 自分の親オブジェクトから EnemyController を探して保持しておく
        _parentController = GetComponentInParent<EnemyController>();

        if (_parentController == null)
        {
            Debug.LogError("親に EnemyController が見つかりません！", this.gameObject);
        }
    }

    /// <summary>
    /// アニメーションイベントから呼び出される関数
    /// </summary>
    public void TriggerAttackHit()
    {
        _parentController?.TriggerAttackHit();
    }
}