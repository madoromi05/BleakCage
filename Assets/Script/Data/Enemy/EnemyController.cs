using UnityEngine;

public class EnemyController : MonoBehaviour
{
    // カードデータを管理する
    EnemyModel model;

    private void Awake()
    {

    }

    public void Init(int cardID)
    {
        // CardModelを作成し、データを適用
        model = new EnemyModel(cardID);
    }
}
