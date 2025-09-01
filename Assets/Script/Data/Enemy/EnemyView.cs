using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI、データをゲームにsetするクラス
/// </summary>

[RequireComponent(typeof(Image))]
public class EnemyView : MonoBehaviour
{
    private Image image;
    private EnemyModel model;

    private void Awake()
    {
        image = GetComponent<Image>();
    }
    public void Show(EnemyModel enemyModel)
    {
        model = enemyModel;

        if (image != null)
        {
            image.sprite = model.EnemySprite;
        }
    }
}