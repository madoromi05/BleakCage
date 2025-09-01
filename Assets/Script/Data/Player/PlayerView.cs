using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class PlayerView : MonoBehaviour
{
    private Image image;
    private PlayerModel model;

    private void Awake()
    {
        image = GetComponent<Image>();
    }
    public void Show(PlayerModel playerModel)
    {
        model = playerModel;

        // image‚ªnull‚Å‚È‚¢‚±‚Æ‚ğ•ÛØ‚µ‚Ä‚©‚çÀs
        if (image != null)
        {
            image.sprite = model.PlayerSprite;
        }
    }
}