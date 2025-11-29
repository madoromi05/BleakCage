using System;
using System.Collections.Generic;
using UnityEngine;

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
    public WeaponModel Model { get; private set; }
    public GameObject Prefab => Model != null ? Model.WeaponPrefab : null;
    public List<CardRuntime> Cards { get; private set; } = new List<CardRuntime>();

    public float attackPower;

    public WeaponRuntime(WeaponModel weaponModel, string instanceID)
    {
        Model = weaponModel;
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
        Cards.Add(card);
        card.SetParent(this);
    }

    /// <summary>
    /// カードを武器から外すメソッド
    /// </summary>
    public void RemoveCard(CardRuntime card)
    {
        if (card == null) return;
        card.SetParent(null);
        Cards.Remove(card);
    }
}