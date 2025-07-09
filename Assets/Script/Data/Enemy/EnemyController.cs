using UnityEngine;

/// <summary>
/// 具体的なデータを管理する
/// </summary>
public class EnemyController : MonoBehaviour
{
    // カードデータを管理する
    EnemyModel model;

    private void Awake()
    {

    }

    public void Init(EnemyEntity enemyEntity)
    {
        // CardModelを作成し、データを適用
        model = new EnemyModel(enemyEntity);
    }
}
