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
    public void Init(CardModel cardModel, float basePower = 1.0f)
    {
        model = cardModel;
        view.Show(model, basePower);
    }

    /// <summary>
    /// カードを選択可能か（暗くしないか）を設定する
    /// </summary>
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