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

    public void Init(int cardID)
    {
        // CardModelを作成し、データを適用
        model = new CardModel(cardID);
        view.Show(model);
    }
}