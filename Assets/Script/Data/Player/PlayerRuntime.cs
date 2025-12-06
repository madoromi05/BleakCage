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
    public float MaxHP => PlayerModel.MaxHP;
    public WeaponRuntime CaracterCardWeapon { get; private set; }
    public PlayerModel PlayerModel { get; private set; }
    public PlayerController PlayerController { get; set; }
    public int Level { get; private set; }
    public StatusEffectHandler StatusHandler { get; private set; }
    public PlayerHPHandler HPHandler { get; private set; }
    public IReadOnlyList<WeaponRuntime> EquippedWeapons => equippedWeapons;
    private readonly List<WeaponRuntime> equippedWeapons = new List<WeaponRuntime>();
    private const float PlayerAttackPower = 10f;

    /// <summary>
    /// Jsonファイルから読み込んだカードのインスタンスを生成するコンストラクタ
    /// </summary>
    public PlayerRuntime(PlayerModel model,string instanceID, int level)
    {
        ID = model.PlayerID;
        InstanceID = Guid.Parse(instanceID);
        CurrentHP = model.PlayerHP;
        this.CurrentHP = model.MaxHP;
        this.PlayerModel = model;
        this.Level = model.PlayerLevel;
        this.StatusHandler = new StatusEffectHandler(model.PlayerName);
        this.HPHandler = new PlayerHPHandler(this);

        // キャラクターカード用の専用武器
        // AttackPowerは仮で10に設定
        var CaracterCardWeaponModel = new WeaponModel(0, "CharacterPersonalSkill", PlayerAttackPower, AttributeType.Bullet, 1.0f, null);
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

    public List<CardRuntime> GetAllCards()
    {
        List<CardRuntime> allCards = new List<CardRuntime>();

        // 直差しカード
        if (this.CaracterCardWeapon != null && this.CaracterCardWeapon.Cards != null)
        {
            allCards.AddRange(this.CaracterCardWeapon.Cards);
        }

        // 装備武器のカード
        foreach (var weapon in this.equippedWeapons)
        {
            if (weapon.Cards != null)
            {
                allCards.AddRange(weapon.Cards);
            }
        }
        return allCards;
    }
}
