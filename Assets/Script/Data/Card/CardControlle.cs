using UnityEngine;

/// <summary>
/// UIにデータをゲームにsetするクラス
/// </summary>
public class CardController : MonoBehaviour
{
    CardView view;
    CardModel model;

    private void Awake()
    {
        view = GetComponent<CardView>();
    }

    // CardModelから直接初期化するメソッドを追加
    public void Init(CardModel cardModel)
    {
        model = cardModel;
        view.Show(model);
    }
}