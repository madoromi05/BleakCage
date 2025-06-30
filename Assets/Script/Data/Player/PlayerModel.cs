using UnityEngine;

public class PlayerModel : MonoBehaviour
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public PlayerEntity.Attribute PlayerAttribute { get; set; }
    public Sprite Icon { get; set; }
    public Sprite SDIcon { get; set; }

    // コンストラクタ（プレイヤーIDを引数にしてデータを読み込む）
    public PlayerModel(int playerId)
    {
        // Resourcesフォルダからプレイヤーデータを取得
        PlayerEntity playerEntity = Resources.Load<PlayerEntity>("PlayerEntityList/Player" + playerId);

        if (playerEntity == null)
        {
            Debug.LogError($"PlayerEntity not found for ID: {playerId}");
            return;
        }

        // 取得したデータをPlayerModelに反映
        PlayerId = playerEntity.PlayerID;
        PlayerName = playerEntity.PlayerName;
        Attack = playerEntity.CharacterAttack;
        Defense = playerEntity.CharacterDefense;
        PlayerAttribute = playerEntity.PlayerAttribute;
        Icon = playerEntity.Icon;
        SDIcon = playerEntity.SDIcon;
    }
}