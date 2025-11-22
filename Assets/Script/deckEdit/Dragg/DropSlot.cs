using UnityEngine;
using UnityEngine.EventSystems;

public class DropSlot : MonoBehaviour, IDropHandler
{
    public enum SlotType { Card, Weapon }
    public SlotType AcceptType;

    // このスロットが誰の、どの武器のスロットかなどを特定するID
    public string OwnerInstanceId; // キャラのInstanceIdなど
    public string ParentWeaponInstanceId; // 武器スロットの場合、武器のInstanceId

    public void OnDrop(PointerEventData eventData)
    {
        if (!DraggableItem.IsEditingMode) return;

        DraggableItem draggedItem = eventData.pointerDrag.GetComponent<DraggableItem>();

        if (draggedItem != null)
        {
            // 型チェック（武器スロットにカードを入れないように）
            if (IsTypeMatch(draggedItem.Type))
            {
                // UI上の親子関係を変更
                draggedItem.transform.SetParent(this.transform);
                draggedItem.transform.localPosition = Vector3.zero;

                DeckEditManager.Instance.OnItemMoved(draggedItem, this);
            }
        }
    }

    private bool IsTypeMatch(string itemType)
    {
        if (AcceptType == SlotType.Card && itemType == "Card") return true;
        if (AcceptType == SlotType.Weapon && itemType == "Weapon") return true;
        return false;
    }
}