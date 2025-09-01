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

    // CardModelから直接初期化するメソッドを追加
    public void Init(PlayerModel playerModel)
    {
        model = playerModel;
        view.Show(model);
    }
}