using UnityEngine;

/// <summary>
/// ゲーム中で使用するプレイヤーモデル（プレイヤーのステータスなど）
/// ScriptableObjectのPlayerEntityから初期化される
/// </summary>
public class PlayerModel
{
    public int PlayerId { get; private set; }
    public string PlayerName { get; private set; }
    public int PlayerLevel { get; private set; }

    public float PlayerHP { get; private set; }
    public float PlayerAttackPower { get; private set; }
    public float PlayerDefensePower { get; private set; }

    public AttackAttributeType PlayerAttribute { get; private set; }

    public Sprite PlayerIcon { get; private set; }
    public Sprite PlayerSDIcon { get; private set; }
    public string PlayerDescription { get; private set; }

    /// <summary>
    /// コンストラクタ：IDからScriptableObjectを読み込んでモデルを生成
    /// </summary>
    public PlayerModel(PlayerEntity playerEntity)
    {
        //初期化
        if (playerEntity == null)
        {
            Debug.LogError("PlayerEntity is null");
            return;
        }

        PlayerId = playerEntity.PlayerId;
        PlayerName = playerEntity.PlayerName;
        PlayerLevel = playerEntity.PlayerLevel;

        PlayerHP = playerEntity.PlayerHP;
        PlayerAttackPower = playerEntity.PlayerAttackPower;
        PlayerDefensePower = playerEntity.PlayerDefensePower;

        PlayerAttribute = playerEntity.PlayerAttribute;

        PlayerIcon = playerEntity.PlayerIcon;
        PlayerSDIcon = playerEntity.PlayerSDIcon;
        PlayerDescription = playerEntity.PlayerDescription;
    }
}
