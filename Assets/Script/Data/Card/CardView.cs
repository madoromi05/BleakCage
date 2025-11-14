using System.Collections.Generic;
using System.Data;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UIで表示させるものを決める
/// 効果等は関係ない
/// </summary>
public class CardView : MonoBehaviour
{
    [SerializeField] private Text Name;
    [SerializeField] private Outline NameOutline;
    [SerializeField] private Text attackAttribute;
    [SerializeField] private Outline AttackAttributeOutline;
    [SerializeField] private Text Description;
    [SerializeField] private Image IconImage;
    [Header("Attribute Icons")]
    [SerializeField] private List<AttributeSpriteMapping> attributeIconMappings;

    private Dictionary<AttributeType, AttributeSpriteMapping> attributeData 
            = new Dictionary<AttributeType, AttributeSpriteMapping>();
    [System.Serializable]
    public class AttributeSpriteMapping
    {
        public AttributeType attribute;
        public Sprite sprite;
        public Color TextOutlineColor;
    }
    private void Awake()
    {
        foreach (var mapping in attributeIconMappings)
        {
            if (!attributeData.ContainsKey(mapping.attribute))
            {
                attributeData.Add(mapping.attribute, mapping);
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
        {
            // CSVから読み込んだ独自の説明文（パッシブ効果など）を取得
            string csvDescription = cardModel.Description;

            // CSVの説明文とモデルデータを使って、最終的な説明文を生成
            Description.text = GenerateFormattedDescription(csvDescription, cardModel);
        }
        // 属性に応じてアイコン画像を切り替える
        if (IconImage != null)
        {
            if (attributeData.TryGetValue(cardModel.Attribute, out AttributeSpriteMapping mapping))
            {
                IconImage.sprite = mapping.sprite;
                if (NameOutline != null)
                {
                    NameOutline.effectColor = mapping.TextOutlineColor;
                }
            }
            else
            {
                // 属性に対応するアイコンがない場合は、CardEntityに設定されているデフォルトのアイコンを使用
                IconImage.sprite = cardModel.CardSprite;
                Debug.LogWarning($"No specific icon found for attribute: {cardModel.Attribute}. Using default icon for card ID: {cardModel.ID}");

                if (NameOutline != null)
                {
                    NameOutline.effectColor = Color.black;
                }
            }
        }
    }

    /// <summary>
    /// CardModelのデータに基づいて、フォーマットされた説明文を生成する
    /// </summary>
    private string GenerateFormattedDescription(string csvInput, CardModel model)
    {
        StringBuilder descriptionBuilder = new StringBuilder();

        // OutputModifierをパーセンテージに変換
        float outputValue = model.OutputModifier * 100;

        switch (model.Attribute)
        {
            // --- 援属性の場合 ---
            case AttributeType.Heal:
                descriptionBuilder.AppendLine($"自身に {outputValue:F0}% 回復");
                break;

            case AttributeType.Defence:
                descriptionBuilder.AppendLine($"自身の防御力を {outputValue:F0}% アップ");
                break;

            // --- 攻撃属性の場合 ---
            case AttributeType.Slash:
            case AttributeType.Blunt:
            case AttributeType.Pierce:
            case AttributeType.Bullet:
            default: // デフォルトは攻撃として扱う
                descriptionBuilder.AppendLine($"出力 {outputValue:F0}%");

                // 攻撃回数 (2回以上のみ表示)
                if (model.AttackCount > 1)
                {
                    descriptionBuilder.AppendLine($"攻撃回数 {model.AttackCount}回");
                }

                // 攻撃対象 (2体以上のみ表示)
                if (model.TargetCount > 1)
                {
                    descriptionBuilder.AppendLine($"攻撃対象 {model.TargetCount}体");
                }

                // 命中率 (100%未満のみ表示)
                if (model.HitRate < 1.0f)
                {
                    descriptionBuilder.AppendLine($"命中率 {model.HitRate * 100:F0}%");
                }
                break;
        }

        return descriptionBuilder.ToString();
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

    private string GetCardTypeName(CardTypeData type)
    {
        switch (type)
        {
            case CardTypeData.Character: return "キャラ";
            case CardTypeData.Weapon:    return "武器";
            case CardTypeData.Universal: return "汎用";
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
            default: return "援";
        }
    }
}
