using UnityEngine;

/// <summary>
/// UI、データをゲームにsetするクラス
/// </summary>
public class CardController : MonoBehaviour
{
    // カードデータを表示する
    CardView view;
    // カードデータを管理する
    CardModel model;

    private void Awake()
    {
        // CardViewを取得
        view = GetComponent<CardView>();
    }

    public void Init(CardEntity cardEntity)
    {
        // CardModelを作成し、データを適用
        model = new CardModel(cardEntity);
        view.Show(model);
    }

    // CardModelから直接初期化するメソッドを追加
    public void Init(CardModel cardModel)
    {
        model = cardModel;
        view.Show(model);
    }
}