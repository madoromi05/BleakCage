using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardView : MonoBehaviour
{
    [SerializeField] TMP_Text nameText;
    //[SerializeField] TMP_Text WeaponDamargeText;
    [SerializeField] Image iconImage;

    public void Show(CardModel cardModel)
    {
        nameText.text = cardModel.name;
        //WeaponDamargeText.text = cardModel.WeaponAttack.ToString();
        iconImage.sprite = cardModel.icon;
    }
}