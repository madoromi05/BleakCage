using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Data Info")]
    // どのデータを表しているか（CardData や WeaponData の参照を持つ）
    public string InstanceId;
    public int DataId;
    public string Type; // "Card" or "Weapon"

    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private RectTransform rectTransform;
    private Canvas parentCanvas;

    // 編集モード中かどうかを制御する静的フラグ（またはManagerから制御）
    public static bool IsEditingMode = false;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsEditingMode) return; // 編集モードでなければドラッグ開始しない

        originalParent = transform.parent;

        // ドラッグ中は描画順を最前面にするためにCanvas直下などへ移動
        transform.SetParent(parentCanvas.transform, true);

        // ドロップ先の検知を妨げないようにブロックを無効化
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsEditingMode) return;

        // マウス位置に追従（Canvasのスケール考慮）
        rectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!IsEditingMode) return;

        canvasGroup.blocksRaycasts = true;

        // ドロップ処理が成功して親が変わっていなければ、元の場所に戻す
        if (transform.parent == parentCanvas.transform)
        {
            transform.SetParent(originalParent);
            rectTransform.anchoredPosition = Vector2.zero;
        }
    }
}