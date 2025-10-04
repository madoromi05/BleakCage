using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// ドラッグアンドドロップのドラッグの部分
/// </summary>
public class PlayerDrag : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    public PlayerModel PlayerData;
    public int PriorityIndex;

    public event Action<PlayerDrag> OnPlayerDragBegin;
    public event Action<PlayerDrag> OnPlayerDragEnd;

    [SerializeField] private DragLineRenderer dragLine;
    public void OnBeginDrag(PointerEventData eventData)
    {
        OnPlayerDragBegin?.Invoke(this);
        dragLine.BeginDragFromScreenPos(eventData.position);
        Debug.Log("Drag開始");
    }
    public void OnDrag(PointerEventData eventData)
    {
        Vector3 currentPos = Camera.main.ScreenToWorldPoint(eventData.position);
        currentPos.z = 0;
        dragLine.UpdateDragFromScreenPos(eventData.position);
    }


    public void OnEndDrag(PointerEventData eventData)
    {
        OnPlayerDragEnd?.Invoke(this);
        dragLine.EndDrag();
        Debug.Log("Drag終了");
    }
}
