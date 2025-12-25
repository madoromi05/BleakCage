using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// フェーズ開始時にターン数とフェーズ名をアニメーション表示する
/// </summary>
public class PhaseAnnouncementUIController : MonoBehaviour
{
    [SerializeField] private GameObject displayContainer;
    [SerializeField] private Image dimmerPanel;
    [SerializeField] private Text phaseText;
    [SerializeField] private float displayDuration = 2.0f;

    /// <summary>
    /// フェーズ情報を表示する
    /// </summary>
    public IEnumerator ShowPhaseAnnouncement(int turnCount, string phaseName)
    {
        phaseText.text = $"Turn {turnCount}\n{phaseName}";
        displayContainer.SetActive(true);
        yield return new WaitForSeconds(displayDuration);
        displayContainer.SetActive(false);
    }
}