using UnityEngine;

public class PlayeryController : MonoBehaviour
{
    // Playerデータを管理する
    PlayerModel model;

    private void Awake()
    {

    }

    public void Init(int PlayerID)
    {
        // PlayerModelを作成し、データを適用
        model = new PlayerModel(PlayerID);
    }
}
