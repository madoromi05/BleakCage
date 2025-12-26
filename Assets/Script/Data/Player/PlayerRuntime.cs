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
    public WeaponRuntime EquippedWeapon { get; private set; }
    public IReadOnlyList<WeaponRuntime> Weapons => equippedWeapons;
    public List<CardModel> Deck { get; set; } = new List<CardModel>();
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
        this.EquipWeapon(CaracterCardWeapon);
    }

    /// <summary>
    /// PlayreRuntimeに武器を装備するメソッド
    /// </summary>
    public void EquipWeapon(WeaponRuntime weapon)
    {
        this.EquippedWeapon = weapon;
        if (weapon == null) return;
        equippedWeapons.Add(weapon);
        weapon.SetParent(this);
    }

    /// <summary>
    /// バトル開始時にデッキに登録すべき全カードを取得する
    /// </summary>
    public List<CardRuntime> GetAllCards()
    {
        List<CardRuntime> cards = new List<CardRuntime>();

        // 装備している武器があれば、そのカード（武器技＋キャラ技）を全て返す
        foreach (var weapon in equippedWeapons)
        {
            if (weapon != null && weapon.Cards != null)
            {
                cards.AddRange(weapon.Cards);
            }
        }

        return cards;
    }
}
