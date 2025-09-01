using UnityEngine;

/// <summary>
/// UI、データをゲームにsetするクラス
/// </summary>
public class PlayerController : MonoBehaviour
{
    PlayerView view;
    PlayerModel model;

    private void Awake()
    {
        view = GetComponent<PlayerView>();
    }

    public void Init(PlayerModel playerModel)
    {
        model = playerModel;
        view.Show(model);
    }
}