using System.Collections.Generic;
using System;

/// <summary>
/// 動的または、UUIDで管理するためのクラス
/// </summary>

public class PlayerRuntime : IAttackComponent
{
    public int ID { get; private set; }
    public System.Guid InstanceID { get; private set; }
    public float CurrentHP { get; set; }

    private readonly float baseAttackPower;
    private readonly IAttackStrategy attackStrategy;

    private readonly List<WeaponRuntime> equippedWeapons = new List<WeaponRuntime>();

    public PlayerRuntime(PlayerModel model, IAttackStrategy strategy)
    {
        ID = model.PlayerID;
        CurrentHP = model.PlayerHP;
        baseAttackPower = model.PlayerAttackPower;
        attackStrategy = strategy;
    }

    /// <summary>
    /// Jsonファイルから読み込んだカードのインスタンスを生成するコンストラクタ
    /// </summary>
    public PlayerRuntime(PlayerModel model, IAttackStrategy strategy, string instanceID)
    {
        ID = model.PlayerID;
        InstanceID = Guid.Parse(instanceID);
        CurrentHP = model.PlayerHP;
        baseAttackPower = model.PlayerAttackPower;
        attackStrategy = strategy;
    }

    public float GetPower()
    {
        return baseAttackPower;
    }

    /// <summary>
    /// PlayreRuntimeに武器を装備するメソッド
    /// </summary>
    public void EquipWeapon(WeaponRuntime weapon)
    {
        if (weapon == null) return;
        equippedWeapons.Add(weapon);
        weapon.SetParent(this);
    }

    /// <summary>
    /// PlayerRuntimeから武器を外すメソッド
    /// </summary>
    public void UnequipWeapon(WeaponRuntime weapon)
    {
        if (weapon == null) return;
        weapon.SetParent(null);
        equippedWeapons.Remove(weapon);
    }
}
