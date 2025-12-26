using UnityEngine;
using UnityEngine.UI;

public class TargetMarkerUI : MonoBehaviour
{
    [SerializeField] private Text keyText; // Prefab“à‚ÌText‚ğƒAƒTƒCƒ“

    public void SetKeyNumber(int keyNumber)
    {
        if (keyText == null) return;
        keyText.text = keyNumber.ToString(); // 1,2,3,4
    }
}
