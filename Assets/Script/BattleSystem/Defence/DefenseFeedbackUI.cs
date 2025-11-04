using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// GUARD, COUNTER, HIT などの防御結果テキストを制御するUIコンポーネント
/// </summary>
public class DefenseFeedbackUI : MonoBehaviour
{
    [SerializeField] private Text defenseFeedbackText;
    [SerializeField] private float feedbackDisplayDuration = 1.5f;

    private Coroutine feedbackCoroutine;

    private void Awake()
    {
        if (defenseFeedbackText != null)
        {
            defenseFeedbackText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 防御結果のテキスト (GUARD, COUNTER, HIT) を表示する
    /// </summary>
    public void ShowDefenseFeedback(string message, Color color)
    {
        if (defenseFeedbackText == null) return;

        // 既存の表示コルーチンが動いていたら停止
        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
        }

        // 新しいコルーチンを開始
        feedbackCoroutine = StartCoroutine(ShowFeedbackCoroutine(message, color));
    }

    private IEnumerator ShowFeedbackCoroutine(string message, Color color)
    {
        defenseFeedbackText.text = message;
        defenseFeedbackText.color = color;
        defenseFeedbackText.gameObject.SetActive(true);

        // ここでアニメーションなどを実装可能

        yield return new WaitForSeconds(feedbackDisplayDuration);

        // フェードアウトなど

        defenseFeedbackText.gameObject.SetActive(false);
        feedbackCoroutine = null;
    }
}