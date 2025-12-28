using System;
using System.Collections.Generic;
using Unity.VisualScripting;
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
    public WeaponRuntime CharacterCardWeapon { get; private set; }
    public PlayerModel PlayerModel { get; private set; }
    public PlayerController PlayerController { get; set; }
    public int Level { get; private set; }
    public StatusEffectHandler StatusHandler { get; private set; }
    public PlayerHpHandler playerHpHandler { get; private set; }
    public WeaponRuntime EquippedWeapon { get; private set; }
    public IReadOnlyList<WeaponRuntime> Weapons => _equippedWeapons;

    public List<CardModel> Deck { get; set; } = new List<CardModel>();
    private readonly List<WeaponRuntime> _equippedWeapons = new List<WeaponRuntime>();
    private const float PlayerAttackPower = 10f;

    /// <summary>
    /// Jsonファイルから読み込んだカードのインスタンスを生成するコンストラクタ
    /// </summary>
        public PlayerRuntime(PlayerModel model, string instanceID, int level)
    {
        PlayerModel = model;
        ID = model.PlayerID;

        if (!Guid.TryParse(instanceID, out Guid guid))
        {
            guid = Guid.NewGuid();
        }
        InstanceID = guid;
        CurrentHP = model.MaxHP;
        Level = level;
        StatusHandler = new StatusEffectHandler(model.PlayerName);
        playerHpHandler = new PlayerHpHandler(this);
    }

    /// <summary>
    /// PlayreRuntimeに武器を装備するメソッド
    /// </summary>
    public void EquipWeapon(WeaponRuntime weapon)
    {
        EquippedWeapon = weapon;
        if (weapon == null) return;

        _equippedWeapons.Add(weapon);
        weapon.SetParent(this);
    }

    /// <summary>
    /// バトル開始時にデッキに登録すべき全カードを取得する
    /// </summary>
    public List<CardRuntime> GetAllCards()
    {
        List<CardRuntime> cards = new List<CardRuntime>();

        foreach (WeaponRuntime weapon in _equippedWeapons)
        {
            if (weapon != null && weapon.Cards != null)
            {
                cards.AddRange(weapon.Cards);
            }
        }

        return cards;
    }
}
