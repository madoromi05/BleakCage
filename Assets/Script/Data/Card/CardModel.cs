using UnityEngine;

public class CardModel
{
    public int CardId { get; set; }
    public string CardName { get; set; }
    public CardEntity.CardType CardType { get; set; }
    public CardEntity.Attribute CardAttribute { get; set; }
    public int BasePower { get; set; }
    public Sprite CardIcon { get; set; }
    public string CardDescription { get; set; }

    // 説明文を置換した結果を返すプロパティ
    public string ResolvedDescription
    {
        get
        {
            return CardDescription
                .Replace("{Type}", CardType.ToString())
                .Replace("{Attribute}", CardAttribute.ToString())
                .Replace("{Power}", BasePower.ToString());
        }
    }

    public CardModel(int cardID)
    {
        CardEntity cardEntity = Resources.Load<CardEntity>("CardEntityList/Card" + cardID);

        CardId = cardEntity.cardId;
        CardName = cardEntity.cardName;
        CardType = cardEntity.cardType;
        CardAttribute = cardEntity.CardAttribute;
        BasePower = cardEntity.basePower;
        CardIcon = cardEntity.CardIcon;
        CardDescription = cardEntity.CardDescription;
    }
}
