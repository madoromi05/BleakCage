using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

/// <summary>
/// ゲーム中で使用するプレイヤーモデル（プレイヤーのステータスなど）
/// ScriptableObjectのPlayerEntityから初期化される
/// </summary>
public class PlayerModel
{
    public int    PlayerID { get; private set; }
    public string PlayerName { get; private set; }
    public int    PlayerLevel { get; private set; }
    public float MaxHP { get; private set; }
    public float  PlayerHP { get; set; }
    public float  PlayerDefensePower { get; private set; }
    public AttributeType PlayerAttribute { get; private set; }
    public GameObject CharacterPrefab { get; private set; }
    public Vector3 InitialRotation { get; private set; }
    public string PlayerDescription { get; private set; }
    public Sprite PlayerIcon { get; private set; }
    public WeaponEntity PlayerWeapon { get; private set; }
    /// <summary>
    /// コンストラクタ：IDからScriptableObjectを読み込んでモデルを生成
    /// </summary>
    public PlayerModel(PlayerEntity entity)
    {
        if (entity == null)
        {
            Debug.LogError("PlayerEntity is null");
            return;
        }

        PlayerID = entity.PlayerId;
        PlayerName = entity.PlayerName;
        PlayerLevel = entity.PlayerLevel;
        PlayerIcon = entity.PlayerIcon;
        PlayerHP = entity.PlayerHP;
        MaxHP = entity.PlayerHP;
        PlayerDefensePower = entity.PlayerDefensePower;
        PlayerAttribute = entity.PlayerAttribute;
        PlayerDescription = entity.PlayerDescription;
        //PlayerAnimator = entity.AnimationSet;
        CharacterPrefab = entity.CharacterPrefab;
        InitialRotation = entity.InitialRotation;
        if (entity.PlayerWeapon != null)
        {
            PlayerWeapon = entity.PlayerWeapon;
        }
    }
}
