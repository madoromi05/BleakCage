using UnityEngine;

public class PlayerModel : MonoBehaviour
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; }
    public int PlayerHP { get; set; }
    public int PlayerAttackPower { get; set; }
    public int PlayerDefensePower { get; set; }
    public PlayerEntity.Attribute PlayerAttribute { get; set; }
    public Sprite PlayerIcon { get; set; }
    public Sprite PlayerSDIcon { get; set; }
    public string PlayerDescription { get; set; }

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
        PlayerId = playerEntity.PlayerId;
        PlayerName = playerEntity.PlayerName;
        PlayerHP = playerEntity.PlayerHP;
        PlayerAttackPower = playerEntity.PlayerAttackPower;
        PlayerDefensePower = playerEntity.PlayerDefensePower;
        PlayerAttribute = playerEntity.PlayerAttribute;
        PlayerIcon = playerEntity.PlayerIcon;
        PlayerSDIcon = playerEntity.PlayerSDIcon;
        PlayerDescription = playerEntity.PlayerDescription;
    }
}
