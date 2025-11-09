using System.Collections.Generic;
using System;
using UnityEngine;

/// <summary>
/// 動的または、UUIDで管理するためのクラス
/// カードと武器、キャラとのつながり、パーティー編成を管理する
/// </summary>

public class PlayerRuntime : IAttackComponent
{
    public int ID { get; private set; }
    public System.Guid InstanceID { get; private set; }
    public float CurrentHP { get; set; }
    public WeaponRuntime CaracterCardWeapon { get; private set; }
    public PlayerModel PlayerModel { get; private set; }
    public PlayerController PlayerController { get; set; }
    public int Level { get; private set; }
    public StatusEffectHandler BuffHandler { get; private set; }
    public PlayerStatsHandler StatsHandler { get; private set; }
    private readonly List<WeaponRuntime> equippedWeapons = new List<WeaponRuntime>();
    private readonly IAttackStrategy attackStrategy;
    private const float PlayerAttackPower = 10f; 

    /// <summary>
    /// Jsonファイルから読み込んだカードのインスタンスを生成するコンストラクタ
    /// </summary>
    public PlayerRuntime(PlayerModel model, IAttackStrategy strategy, string instanceID, int level)
    {
        ID = model.PlayerID;
        InstanceID = Guid.Parse(instanceID);
        CurrentHP = model.PlayerHP;
        attackStrategy = strategy;
        this.PlayerModel = model;
        this.Level = model.PlayerLevel;
        this.BuffHandler = new StatusEffectHandler(this.PlayerModel);
        this.StatsHandler = new PlayerStatsHandler(this.PlayerModel);

        // キャラクターカード用の専用武器
        // AttackPowerは仮で10に設定
        var CaracterCardWeaponModel = new WeaponModel(0, "CharacterPersonalSkill", PlayerAttackPower, AttributeType.Bullet, 1.0f);
        CaracterCardWeapon = new WeaponRuntime(CaracterCardWeaponModel, System.Guid.NewGuid().ToString());
        this.EquipWeapon(CaracterCardWeapon);
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

    /// <summary>
    /// デバッグ用に内部のリストがnullでないか確認するメソッド
    /// </summary>
    public void CheckInternalListForDebug()
    {
        Debug.Log($"--- Inside PlayerRuntime Check ---");
        Debug.Log($"Checking equippedWeapons list: {(equippedWeapons == null ? "IS NULL!!! <- おそらくこれが原因です" : "is OK")}");
    }
}
