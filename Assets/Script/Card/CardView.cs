using TMPro;
using UnityEngine;
using UnityEngine.UI;

//UI‚Å•\¦‚³‚¹‚é‚à‚Ì‚ğŒˆ‚ß‚é
//Œø‰Ê“™‚ÍŠÖŒW‚È‚¢
public class CardView : MonoBehaviour
{
    [SerializeField] Text CardId;
    [SerializeField] Text attackAttribute;//UŒ‚‘®«
    [SerializeField] Image iconImage;

    public void Show(CardModel cardModel)
    {
        CardId.text = cardModel.CardId.ToString();
        attackAttribute.text = cardModel.Attribute.ToString();
        iconImage.sprite = cardModel.Icon;
    }
}