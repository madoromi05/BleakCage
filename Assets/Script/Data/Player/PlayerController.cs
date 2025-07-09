using UnityEngine;

public class PlayeryController : MonoBehaviour
{
    // Playerデータを管理する
    PlayerModel model;

    private void Awake()
    {

    }

    public void Init(PlayerEntity playerEntity)
    {
        // CardModelを作成し、データを適用
        model = new PlayerModel(playerEntity);
    }
}
