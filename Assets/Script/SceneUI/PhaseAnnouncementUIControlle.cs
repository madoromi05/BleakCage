using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// フェーズ開始時にターン数とフェーズ名をアニメーション表示する
/// </summary>
public class PhaseAnnouncementUIController : MonoBehaviour
{
    [SerializeField] private GameObject displayContainer; // DimmerPanelとPhaseTextの親
    [SerializeField] private Image dimmerPanel;
    [SerializeField] private Text phaseText;
    [SerializeField] private float displayDuration = 2.0f;

    /// <summary>
    /// フェーズ情報を表示する
    /// </summary>
    /// <param name="turnCount">現在のターン数</param>
    /// <param name="phaseName">表示するフェーズ名</param>
    /// <returns>コルーチン</returns>
    public IEnumerator ShowPhaseAnnouncement(int turnCount, string phaseName)
    {
        // テキストを設定
        phaseText.text = $"Turn {turnCount}\n{phaseName}";

        // UIを表示
        displayContainer.SetActive(true);

        // 指定時間待機
        yield return new WaitForSeconds(displayDuration);

        // UIを非表示
        displayContainer.SetActive(false);
    }
}