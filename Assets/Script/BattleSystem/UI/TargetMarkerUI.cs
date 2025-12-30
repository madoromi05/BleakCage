using UnityEngine;
using UnityEngine.UI;

public class TargetMarkerUI : MonoBehaviour
{
    [SerializeField] private Text keyText;

    public void SetKeyNumber(int keyNumber)
    {
        if (keyText == null) return;
        keyText.text = keyNumber.ToString();
    }
}