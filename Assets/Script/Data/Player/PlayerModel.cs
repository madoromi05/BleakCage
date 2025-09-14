using UnityEngine;

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
    public float  PlayerAttackPower { get; private set; }
    public float  PlayerDefensePower { get; private set; }
    public AttributeType PlayerAttribute { get; private set; }
    public Sprite PlayerSprite { get; private set; }
    public string PlayerDescription { get; private set; }
    public AnimatorSet PlayerAnimator { get; private set; }

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

        PlayerHP = playerEntity.PlayerHP;
        PlayerAttackPower = playerEntity.PlayerAttackPower;
        PlayerDefensePower = playerEntity.PlayerDefensePower;

        PlayerAttribute = playerEntity.PlayerAttribute;

        PlayerSprite = playerEntity.PlayerSprite;
        PlayerDescription = playerEntity.PlayerDescription;
        PlayerAnimator = playerEntity.AnimationSet;
    }
}
