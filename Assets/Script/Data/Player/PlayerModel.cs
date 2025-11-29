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
    public float  PlayerHP { get; set; }
    public float  PlayerDefensePower { get; private set; }
    public AttributeType PlayerAttribute { get; private set; }
    public GameObject CharacterPrefab { get; private set; }
    public Vector3 InitialRotation { get; private set; }
    public string PlayerDescription { get; private set; }
    public AnimatorSet PlayerAnimator { get; private set; }
    public Sprite PlayerIcon { get; private set; }
    public WeaponModel PlayerWeapon { get; private set; }
    /// <summary>
    /// コンストラクタ：IDからScriptableObjectを読み込んでモデルを生成
    /// </summary>
    public PlayerModel(PlayerEntity playerEntity)
    {
        if (playerEntity == null)
        {
            Debug.LogError("PlayerEntity is null");
            return;
        }

        PlayerID = playerEntity.PlayerId;
        PlayerName = playerEntity.PlayerName;
        PlayerLevel = playerEntity.PlayerLevel;
        PlayerIcon = playerEntity.PlayerIcon;
        PlayerHP = playerEntity.PlayerHP;
        PlayerDefensePower = playerEntity.PlayerDefensePower;

        PlayerAttribute = playerEntity.PlayerAttribute;

        PlayerDescription = playerEntity.PlayerDescription;
        PlayerAnimator = playerEntity.AnimationSet;
        CharacterPrefab = playerEntity.CharacterPrefab;
        InitialRotation = playerEntity.InitialRotation;
        if (playerEntity.PlayerWeapon != null)
        {
            PlayerWeapon = new WeaponModel(playerEntity.PlayerWeapon);
        }
    }
}
