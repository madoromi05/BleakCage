using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// テキストのスクロール
/// TMP_Textにアタッチしてください
/// </summary>
[RequireComponent(typeof(CanvasGroup))]

public class CardTextScroller : MonoBehaviour
{
    [SerializeField] private float scrollSpeed = 100f; // スクロール速度
    [SerializeField] private float scrollFinishLineAddValue = 50; // スクロール終了判定までのマージン、0だと全て表示された時点で終了する
    [SerializeField] private float waitTimeBeforeScroll = 2f; // スクロール開始前の待機時間
    [SerializeField] private float waitTimeAfterScroll = 2f; // スクロール完了後、フェードアウトするまでの待機時間
    [SerializeField] private float fadeDuration = 0.4f; // テキストのフェードインアウト時間
    [SerializeField] private float waitTimeFade = 0.2f; // フェードアウトしてからフェードインするまでの待機時間

    private CanvasGroup canvasGroup;
    private RectTransform textRectTransform;
    private RectTransform parentRectTransform;
    private Vector3 startPosition; // スタート位置
    private float scrollValue; // スクロール値
    private float finishLineValue; // スクロールが停止する位置、scrollValueがこの値を超えたら停止する
    private float textWidth;
    private float parentWidth;
    private bool isScrolling = false;

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        if (!isScrolling) return;

        // 停止位置までスクロールする
        if (scrollValue + parentWidth >= finishLineValue)
        {
            isScrolling = false;
            StartCoroutine(Fade(0, waitTimeAfterScroll));
            Invoke(nameof(ResetScrollPosition), waitTimeAfterScroll + fadeDuration + waitTimeFade);
        }
        else
        {
            scrollValue += scrollSpeed * Time.deltaTime;
            textRectTransform.anchoredPosition = new Vector3(startPosition.x - scrollValue, startPosition.y, startPosition.z);
        }
    }


    /// <summary>
    /// スクロールの初期設定
    /// </summary>
    private void Initialize()
    {
        if (!TryGetComponent<TMP_Text>(out var textComponent))
        {
            Debug.LogWarning("TMP_Textがアタッチされていないのでスクロールは機能しません");
            return;
        }

        // ContentSizeFitter手動更新
        var contentSizeFitter = GetComponent<ContentSizeFitter>();
        contentSizeFitter.SetLayoutHorizontal();
        contentSizeFitter.SetLayoutVertical();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentSizeFitter.GetComponent<RectTransform>());

        canvasGroup = GetComponent<CanvasGroup>();
        textRectTransform = textComponent.GetComponent<RectTransform>();
        parentRectTransform = textComponent.transform.parent.GetComponent<RectTransform>();
        textWidth = textRectTransform.rect.width;
        parentWidth = parentRectTransform.rect.width;
        startPosition = textRectTransform.anchoredPosition;
        finishLineValue = textWidth + startPosition.x + scrollFinishLineAddValue;

        if (textWidth + startPosition.x > parentWidth)
        {
            Invoke(nameof(StartScrolling), waitTimeBeforeScroll);
        }
    }


    /// <summary>
    /// スクロール開始
    /// </summary>
    private void StartScrolling()
    {
        scrollValue = 0;
        isScrolling = true;
    }


    /// <summary>
    /// スクロールリセット
    /// </summary>
    private void ResetScrollPosition()
    {
        textRectTransform.anchoredPosition = startPosition;
        StartCoroutine(Fade(1, 0));
        Invoke(nameof(StartScrolling), waitTimeBeforeScroll + fadeDuration);
    }


    /// <summary>
    /// フェード処理コルーチン
    /// </summary>
    private IEnumerator Fade(float targetAlpha, float delayTime)
    {
        var elapsedTime = 0f;
        var startAlpha = canvasGroup.alpha;

        yield return new WaitForSeconds(delayTime);
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;
    }

}
