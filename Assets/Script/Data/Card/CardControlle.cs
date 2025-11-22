using UnityEngine;

/// <summary>
/// UIにデータをゲームにsetするクラス
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class CardController : MonoBehaviour
{
    private CardView view;
    private CardModel model;
    private CanvasGroup canvasGroup;
    private float carDefaultd = 1.0f;
    private float cardDarken = 0.3f;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        view = GetComponent<CardView>();
    }

    // CardModelから直接初期化するメソッドを追加
    public void Init(CardModel cardModel)
    {
        model = cardModel;
        view.Show(model);
    }

    /// <summary>
    /// カードを選択可能か（暗くしないか）を設定する
    /// </summary>
    /// <param name="interactable">true: 通常表示 (Alpha=1), false: 暗くする (Alpha=0.5)</param>
    public void SetInteractable(bool interactable)
    {
        if (canvasGroup == null)
        {
            Debug.LogError("CardControllerにCanvasGroupがアタッチされていません！");
            return;
        }

        if (interactable)
        {
            canvasGroup.alpha = carDefaultd;
        }
        else
        {
            // 暗さを変更できます
            canvasGroup.alpha = cardDarken;
        }
    }
}