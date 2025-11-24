using System.Collections.Generic;
using System;

/// <summary>
///　動的または、UUIDで管理するためのクラス
/// </summary>
public class WeaponRuntime : IAttackComponent
{
    public int ID { get; private set; }
    public System.Guid InstanceID { get; private set; }
    public AttributeType Attribute { get; private set; }
    public float PeakyCoefficient { get; private set; }
    public PlayerRuntime ParentPlayer { get; private set; }
    public List<CardRuntime> Cards { get; private set; }
    public float attackPower;
    private readonly List<CardRuntime> slottedCards = new List<CardRuntime>();

    public WeaponRuntime(WeaponModel weaponModel, string instanceID)
    {
        ID = weaponModel.ID;
        InstanceID = Guid.Parse(instanceID);
        attackPower = weaponModel.AttackPower;
        Attribute = weaponModel.Attribute;
        PeakyCoefficient = weaponModel.PeakyCoefficient;
    }

    /// <summary>
    /// プレイヤーへの参照を設定する内部メソッド
    /// </summary>
    internal void SetParent(PlayerRuntime player)
    {
        ParentPlayer = player;
    }
    /// <summary>
    /// カードを武器に装着するメソッド
    /// </summary>
    public void AddCard(CardRuntime card)
    {
        if (card == null) return;
        slottedCards.Add(card);
        card.SetParent(this);
    }

    /// <summary>
    /// カードを武器から外すメソッド
    /// </summary>
    public void RemoveCard(CardRuntime card)
    {
        if (card == null) return;
        card.SetParent(null);
        slottedCards.Remove(card);
    }
}