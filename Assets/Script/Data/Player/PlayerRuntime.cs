using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// プレイヤーのランタイムクラス
/// プレイヤーのランタイムクラス
/// </summary>
public class PlayerRuntime
{
    public int Identifyer { get; private set; }
    public string Name { get; private set; }
    public float CurrentHP { get; set; }
    private readonly float _baseAttackPower;
    private readonly IAttackStrategy _attackStrategy;

    private readonly List<WeaponRuntime> _equippedWeapons = new List<WeaponRuntime>();
    public IEnumerable<WeaponRuntime> EquippedWeapons => _equippedWeapons.AsReadOnly();

    public PlayerRuntime(PlayerModel model, IAttackStrategy strategy)
    {
        Identifyer = model.PlayerId;
        Name = model.PlayerName;
        CurrentHP = model.PlayerHP;
        _baseAttackPower = model.PlayerAttackPower;
        _attackStrategy = strategy;
    }

    public float GetPower()
    {
        return _baseAttackPower;
    }

    public void EquipWeapon(WeaponRuntime weapon)
    {
        if (weapon == null) return;
        _equippedWeapons.Add(weapon);
        weapon.SetParent(this); // 武器に親（このプレイヤー）を教える
    }

    public void UnequipWeapon(WeaponRuntime weapon)
    {
        if (weapon == null) return;
        weapon.SetParent(null); // 親子関係を解除
        _equippedWeapons.Remove(weapon);
    }
}
