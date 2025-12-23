using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UIで表示させるものを決める
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

    public void Show(CardModel cardModel, float playerBasePower = 10f)
    {
        if (Name != null) Name.text = cardModel.Name;

        // 属性名表示
        if (attackAttribute != null)
            attackAttribute.text = GetAttributeName(cardModel.Attribute);

        // 説明文生成
        if (Description != null)
        {
            string csvDescription = cardModel.Description;
            Description.text = GenerateFormattedDescription(csvDescription, cardModel, playerBasePower);
        }

        // アイコン・色設定
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
                // マッピングがない場合のデフォルト（黒など）
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
    private string GenerateFormattedDescription(string csvInput, CardModel model, float basePower)
    {
        StringBuilder descriptionBuilder = new StringBuilder();
        float baseValue = model.OutputModifier * 100;
        float damageValue = basePower * model.OutputModifier;

        switch (model.Attribute)
        {
            // 支援・回復・バフ
            case AttributeType.Heal:
                descriptionBuilder.AppendLine($"自身に {baseValue:F0}% 回復");
                break;
            case AttributeType.DefenseBuff:
                descriptionBuilder.AppendLine($"自身の防御力を {baseValue:F0}% アップ");
                break;

            case AttributeType.AttackBuff:
                descriptionBuilder.AppendLine($"自身の攻撃力を {baseValue:F0}% アップ");
                break;

            // 攻撃属性
            default:
                if (model.AttackCount > 1)
                {
                    descriptionBuilder.AppendLine($"ダメージ {damageValue:F0} × {model.AttackCount}");
                }
                else
                {
                    descriptionBuilder.AppendLine($"ダメージ {damageValue:F0}");
                }

                // 範囲攻撃の補足
                if (model.TargetScope == CardTargetScope.Random)
                {
                    descriptionBuilder.AppendLine($"(対象: ランダム{model.TargetCount}体)");
                }
                else if (model.TargetScope == CardTargetScope.All)
                {
                    descriptionBuilder.AppendLine("(対象: 全体)");
                }

                // 命中率 (100%未満のみ表示)
                if (model.HitRate < 1.0f)
                {
                    descriptionBuilder.AppendLine($"命中率 {model.HitRate * 100:F0}%");
                }
                break;
        }

        // 状態異常,バフ効果の表示
        if (model.StatusEffect.Type != StatusEffectType.None)
        {
            string statusName = GetStatusEffectName(model.StatusEffect.Type);
            int stack = model.StatusEffect.InflictStacks;
            int turn = model.StatusEffect.Duration;
            descriptionBuilder.AppendLine($"【{statusName}】{stack} ({turn}ターン)");
        }

        return descriptionBuilder.ToString();
    }

    private string GetAttributeName(AttributeType attr)
    {
        switch (attr)
        {
            case AttributeType.Slash: return "斬";
            case AttributeType.Blunt: return "鈍";
            case AttributeType.Pierce: return "突";
            case AttributeType.Bullet: return "弾";
            case AttributeType.Heal: return "癒";
            case AttributeType.AttackBuff: return "攻強";
            case AttributeType.DefenseBuff: return "守強";
            default: return "他";
        }
    }

    private string GetStatusEffectName(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Fracture: return "破砕";
            case StatusEffectType.Laceration: return "損傷";
            case StatusEffectType.Meltdown: return "熔鉄";
            case StatusEffectType.Cover: return "援護";
            case StatusEffectType.Target: return "目標";
            case StatusEffectType.DefenceUp: return "防御UP";
            case StatusEffectType.AttackUp: return "攻撃UP";
            default: return type.ToString();
        }
    }
}