using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardView : MonoBehaviour
{
    [SerializeField] Text CardID;
    //[SerializeField] TMP_Text WeaponDamargeText;
    [SerializeField] Image iconImage;

    public void Show(CardModel cardModel)
    {
        CardID.text = cardModel.CardId.ToString();
        iconImage.sprite = cardModel.icon;
    }
}