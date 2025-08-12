using System.Collections.Generic;
using System.Linq;

/// <summary>
///　武器のランタイムクラス
/// </summary>
public class WeaponRuntime : IAttackComponent
{
    public int Identifyer { get; private set; }
    public string Name { get; private set; }
    public AttributeType Attribute { get; private set; }
    public float PeakyCoefficient { get; private set; }
    private readonly float _baseAttackPower;

    // ★追加：自身を装備しているプレイヤーへの参照
    public PlayerRuntime ParentPlayer { get; private set; }

    private readonly List<CardRuntime> _slottedCards = new List<CardRuntime>();
    public IEnumerable<CardRuntime> SlottedCards => _slottedCards.AsReadOnly();

    public WeaponRuntime(WeaponModel model)
    {
        Identifyer = model.WeaponId;
        Name = model.WeaponName;
        _baseAttackPower = model.WeaponAttackPower;
        Attribute = model.WeaponAttribute;
        PeakyCoefficient = model.PeakyCoefficient;
    }

    public float GetPower()
    {
        float totalCardPower = _slottedCards.Sum(card => card.GetPower());
        return _baseAttackPower + totalCardPower;
    }

    /// <summary>
    /// 親オブジェクト（プレイヤー）への参照を設定する内部メソッド
    /// </summary>
    internal void SetParent(PlayerRuntime player)
    {
        ParentPlayer = player;
    }

    public void AddCard(CardRuntime card)
    {
        if (card == null) return;
        _slottedCards.Add(card);
        card.SetParent(this); // ★カードに親（この武器）を教える
    }

    public void RemoveCard(CardRuntime card)
    {
        if (card == null) return;
        card.SetParent(null); // ★親子関係を解除
        _slottedCards.Remove(card);
    }
}