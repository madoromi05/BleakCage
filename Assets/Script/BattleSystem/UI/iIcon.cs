using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// チュートリアル説明文表示
/// </summary>
public class iIcon : MonoBehaviour
{
    [SerializeField] private GameObject infoText;
    [SerializeField] private GameObject infoPanel;

    private void Start()
    {
        if (infoText != null)
            infoText.SetActive(false);
            infoPanel.SetActive(false);
    }

    public void ToggleInfo()
    {
        if (infoText != null)
            infoText.SetActive(!infoText.activeSelf);
            infoPanel.SetActive(!infoText.activeSelf);
    }
}
