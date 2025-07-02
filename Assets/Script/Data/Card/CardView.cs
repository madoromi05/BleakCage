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
    [SerializeField] TextMeshProUGUI Description;
    [SerializeField] Image iconImage;

    public void Show(CardModel cardModel)
    {
        CardName.text = cardModel.CardName;
        attackAttribute.text = cardModel.CardAttribute.ToString();
        Description.text = cardModel.ResolvedDescription;
        iconImage.sprite = cardModel.CardIcon;
    }
}