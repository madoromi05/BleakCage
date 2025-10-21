using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UIで表示させるものを決める
/// 効果等は関係ない
/// </summary>
public class CardView : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI Name;
    [SerializeField] TextMeshProUGUI attackAttribute;
    [SerializeField] TextMeshProUGUI Description;
    [SerializeField] Image IconImage;
    [Header("Attribute Icons")]
    [SerializeField] private List<AttributeSpriteMapping> attributeIconMappings;

    private Dictionary<AttributeType, Sprite> attributeIcons = new Dictionary<AttributeType, Sprite>();

    [System.Serializable]
    public class AttributeSpriteMapping
    {
        public AttributeType attribute;
        public Sprite sprite;
    }
    private void Awake()
    {
        // Inspectorで設定されたリストからDictionaryを初期化
        foreach (var mapping in attributeIconMappings)
        {
            if (!attributeIcons.ContainsKey(mapping.attribute))
            {
                attributeIcons.Add(mapping.attribute, mapping.sprite);
            }
        }
    }

    public void Show(CardModel cardModel)
    {
        if (Name != null) Name.text = cardModel.Name;

        // 属性名（日本語）だけを表示
        if (attackAttribute != null)
            attackAttribute.text = GetAttributeName(cardModel.Attribute);

        // 説明文テンプレートを置換
        if (Description != null)
            Description.text = ReplacePlaceholders(cardModel.Description, cardModel);

        // 属性に応じてアイコン画像を切り替える
        if (IconImage != null)
        {
            if (attributeIcons.TryGetValue(cardModel.Attribute, out Sprite attributeSprite))
            {
                IconImage.sprite = attributeSprite;
            }
            else
            {
                // 属性に対応するアイコンがない場合は、CardEntityに設定されているデフォルトのアイコンを使用
                IconImage.sprite = cardModel.CardSprite;
                Debug.LogWarning($"No specific icon found for attribute: {cardModel.Attribute}. Using default icon for card ID: {cardModel.ID}");
            }
        }
    }

    private string ReplacePlaceholders(string input, CardModel model)
    {
        return input
            .Replace("{Type}", GetCardTypeName(model.Type))
            .Replace("{Attribute}", GetAttributeName(model.Attribute))
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

    private string GetAttributeName(AttributeType attr)
    {
        switch (attr)
        {
            case AttributeType.Slash:  return "斬";
            case AttributeType.Blunt:  return "鈍";
            case AttributeType.Pierce: return "突";
            case AttributeType.Bullet: return "弾";
            default:                         return attr.ToString();
        }
    }
}
