using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI、データをゲームにsetするクラス
/// </summary>

[RequireComponent(typeof(SpriteRenderer))]
public class EnemyView : MonoBehaviour
{
    private SpriteRenderer enemySpriteRenderer;
    private EnemyModel model;

    private void Awake()
    {
        enemySpriteRenderer = GetComponent<SpriteRenderer>();
    }
    public void Show(EnemyModel enemyModel)
    {
        model = enemyModel;

        if (enemySpriteRenderer != null)
        {
            enemySpriteRenderer.sprite = model.EnemySprite;
        }
    }
}