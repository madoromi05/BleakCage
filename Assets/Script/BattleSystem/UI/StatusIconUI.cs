using UnityEngine;
using UnityEngine.UI;

public class StatusIconUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Text stackText;

    public void Setup(Sprite sprite, int stackCount)
    {
        iconImage.sprite = sprite;

        // スタックが1より大きければ数字を表示、1なら非表示
        if (stackCount > 1)
        {
            stackText.text = stackCount.ToString();
            stackText.gameObject.SetActive(true);
        }
        else
        {
            stackText.gameObject.SetActive(false);
        }
    }
}