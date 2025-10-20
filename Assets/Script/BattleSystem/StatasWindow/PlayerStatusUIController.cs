using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// キャラクターのステータス表示UI全体を管理する
/// </summary>
public class PlayerStatusUIController : MonoBehaviour
{
    [SerializeField] private Image characterIcon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI defenseText;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private float hpAnimationDuration = 0.5f;
    [SerializeField] private Image background;
    private Color originalBackgroundColor;
    private Coroutine hpAnimationCoroutine;
    private PlayerRuntime layerRuntime;
    private float maxHP;

    public int PlayerID { get; private set; }

    private void Awake()
    {
        // 起動時に元の背景色を記憶しておく
        if (background != null)
        {
            originalBackgroundColor = background.color;
        }
    }

    /// <summary>
    /// Playerのデータを使ってUIを初期設定する
    /// </summary>
    public void SetPlayerStatus(PlayerRuntime playerRuntime)
    {
        PlayerModel model = playerRuntime.PlayerModel;

        this.PlayerID = model.PlayerID;
        nameText.text = model.PlayerName;
        levelText.text = model.PlayerLevel.ToString();
        attackText.text = (model.PlayerLevel * 10).ToString();
        defenseText.text = model.PlayerDefensePower.ToString();

        if (model.PlayerIcon != null)
        {
            characterIcon.sprite = model.PlayerIcon;
        }
        else
        {
            Debug.LogWarning($"PlayerModel '{model.PlayerName}' にUI用のPlayerIconが設定されていません。", this.gameObject);
        }

        this.maxHP = model.PlayerHP;
        hpSlider.maxValue = model.PlayerHP;
        hpSlider.value = playerRuntime.CurrentHP;
    }

    /// <summary>
    /// HPバーの表示を更新する
    /// </summary>
    public void UpdateHP(float currentHP)
    {
        // 既にHP減少アニメーションが実行中なら、それを停止する
        if (hpAnimationCoroutine != null)
        {
            StopCoroutine(hpAnimationCoroutine);
        }

        float hpPercentage = (maxHP > 0) ? (currentHP / maxHP) * 100f : 0f;
        hpAnimationCoroutine = StartCoroutine(AnimateHPBarCoroutine(hpPercentage));
    }

    /// <summary>
    /// HPバーをアニメーションさせるコルーチン
    /// </summary>
    private IEnumerator AnimateHPBarCoroutine(float targetHP)
    {
        float startHP = hpSlider.value; // アニメーション開始時のHP
        float elapsedTime = 0f;         // 経過時間

        while (elapsedTime < hpAnimationDuration)
        {
            // 経過時間に応じて、開始時のHPと目標のHPの間を線形補間する
            elapsedTime += Time.deltaTime;
            float newHP = Mathf.Lerp(startHP, targetHP, elapsedTime / hpAnimationDuration);
            hpSlider.value = newHP;
            yield return null;
        }

        hpSlider.value = targetHP;
        hpAnimationCoroutine = null;
        Debug.Log($"Animating HP from {hpSlider.value} to {targetHP}");
    }

    /// <summary>
    /// 指定した色で背景をハイライトする
    /// </summary>
    public void SetHighlight(Color highlightColor)
    {
        if (background != null)
        {
            background.color = highlightColor;
        }
    }

    /// <summary>
    /// 背景色を元の色に戻す
    /// </summary>
    public void ResetHighlight()
    {
        if (background != null)
        {
            background.color = originalBackgroundColor;
        }
    }
}