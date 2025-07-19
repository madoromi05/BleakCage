using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UIで表示させるものを決める
/// 効果等は関係ない
/// </summary>
public class CardView : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI CardName;
    [SerializeField] TextMeshProUGUI attackAttribute;   // 攻撃属性のみ表示
    [SerializeField] TextMeshProUGUI Description;
    [SerializeField] Image IconImage;

    public void Show(CardModel cardModel)
    {
        if (cardModel == null)
        {
            Debug.LogError("CardModel is null");
            return;
        }

        if (CardName != null)
            CardName.text = cardModel.CardName;

        // 属性名（日本語）だけを表示
        if (attackAttribute != null)
            attackAttribute.text = GetAttributeName(cardModel.CardAttribute);

        // 説明文テンプレートを置換
        if (Description != null)
            Description.text = ReplacePlaceholders(cardModel.CardDescription, cardModel);

        if (IconImage != null)
            IconImage.sprite = cardModel.CardIcon;
    }

    private string ReplacePlaceholders(string input, CardModel model)
    {
        return input
            .Replace("{Type}", GetCardTypeName(model.CardType))
            .Replace("{Attribute}", GetAttributeName(model.CardAttribute))
            .Replace("{HitRate}", Mathf.RoundToInt(model.HitRate * 100).ToString() + "%")
            .Replace("{AttackCount}", model.AttackCount.ToString())
            .Replace("{TargetCount}", model.TargetCount.ToString())
            .Replace("{Passive}", model.IsPassive ? "常時発動" : "使用型")
            .Replace("{OutputMod}", model.OutputModifier.ToString("F2"));
    }

    private string GetCardTypeName(CardEntity.CardTypeData type)
    {
        switch (type)
        {
            case CardEntity.CardTypeData.Character: return "キャラ";
            case CardEntity.CardTypeData.Weapon:    return "武器";
            case CardEntity.CardTypeData.Universal: return "汎用";
            default:                                return type.ToString();
        }
    }

    private string GetAttributeName(AttackAttributeType attr)
    {
        switch (attr)
        {
            case AttackAttributeType.Slash:  return "斬";
            case AttackAttributeType.Blunt:  return "鈍";
            case AttackAttributeType.Pierce: return "突";
            case AttackAttributeType.Bullet: return "弾";
            default:                         return attr.ToString();
        }
    }
}
