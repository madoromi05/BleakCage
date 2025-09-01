using UnityEngine;

/// <summary>
/// 具体的なデータを管理する
/// </summary>
public class EnemyController : MonoBehaviour
{
    EnemyView view;
    EnemyModel model;

    private void Awake()
    {
        view = GetComponent<EnemyView>();
    }

    public void Init(EnemyModel enemyModel)
    {
        model = enemyModel;
        view.Show(model);
    }
}
