using UnityEngine;

/// <summary>
///     カードのデータを管理するクラス
/// </summary>

public class CardModel
{
    public int CardId { get; set; }
    public string CardName { get; set; }
    public CardEntity.CardType CardType { get; set; }
    public CardEntity.Attribute CardAttribute { get; set; }
    public int BasePower { get; set; }
    public Sprite Icon { get; set; }
    public string Description { get; set; }

    // コンストラクタ（カードIDを引数にしてデータを読み込む）
    public CardModel(int cardID)
    {
        // Resourcesフォルダからカードデータを取得
        CardEntity cardEntity = Resources.Load<CardEntity>("CardEntityList/Card" + cardID);

        // 取得したデータをCardModelに反映
        CardId = cardEntity.cardID;
        CardName = cardEntity.cardName;
        CardType = cardEntity.cardType;
        CardAttribute = cardEntity.CardAttribute;
        BasePower = cardEntity.basePower;
        Icon = cardEntity.icon;
        Description = cardEntity.description;
    }
}