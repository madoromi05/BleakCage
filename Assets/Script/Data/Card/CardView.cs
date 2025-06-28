using TMPro;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// UI‚Å•\¦‚³‚¹‚é‚à‚Ì‚ğŒˆ‚ß‚é
/// Œø‰Ê“™‚ÍŠÖŒW‚È‚¢
/// </summary>

public class CardView : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI CardName;
    [SerializeField] TextMeshProUGUI attackAttribute;   //UŒ‚‘®«
    [SerializeField] Image iconImage;

    public void Show(CardModel cardModel)
    {
        CardName.text = cardModel.CardName;
        attackAttribute.text = cardModel.Attribute.ToString();
        iconImage.sprite = cardModel.Icon;
    }
}