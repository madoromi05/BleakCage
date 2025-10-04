using UnityEngine;
using UnityEngine.EventSystems;
using System;

/// <summary>
/// ドラッグアンドドロップのドロップの部分
/// </summary>
public class EnemyDrop : MonoBehaviour, IDropHandler
{
    public EnemyModel EnemyData;

    public event Action<PlayerDrag, EnemyModel> OnEnemyDropped;

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("EnemyDropはよばれてるぜ");
        var playerDrag = eventData.pointerDrag?.GetComponent<PlayerDrag>();
        if (playerDrag != null)
        {
            Debug.Log("moe");
            OnEnemyDropped?.Invoke(playerDrag, EnemyData);
        }
    }
}
