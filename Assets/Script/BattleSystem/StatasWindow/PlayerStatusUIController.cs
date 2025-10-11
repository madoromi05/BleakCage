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
    [SerializeField] private Image flashOverlay; // 点滅させるためのUI画像
    private Coroutine flashingCoroutine;

    private void Awake()
    {
        // ゲーム開始時は点滅用の画像を非表示にしておく
        if (flashOverlay != null)
        {
            flashOverlay.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Playerのデータを使ってUIを初期設定する
    /// </summary>
    public void SetPlayerStatus(PlayerRuntime playerRuntime)
    {
        PlayerModel model = playerRuntime.PlayerModel;

        nameText.text = model.PlayerName;
        levelText.text = $"Lv.{model.PlayerLevel}";
        defenseText.text = model.PlayerDefensePower.ToString();

        if (model.PlayerSprite != null)
        {
            characterIcon.sprite = model.PlayerSprite;
        }

        hpSlider.maxValue = model.PlayerHP;
        hpSlider.value = playerRuntime.CurrentHP;
    }

    /// <summary>
    /// HPバーの表示を更新する
    /// </summary>
    public void UpdateHP(float currentHP)
    {
        hpSlider.value = currentHP;
    }

    /// <summary>
    /// 指定した色で点滅を開始する
    /// </summary>
    public void StartFlashing(Color flashColor)
    {
        if (flashOverlay == null) return;

        if (flashingCoroutine != null)
        {
            StopCoroutine(flashingCoroutine);
        }
        flashOverlay.color = flashColor;
        flashingCoroutine = StartCoroutine(FlashCoroutine());
    }

    /// <summary>
    /// 点滅を停止する
    /// </summary>
    public void StopFlashing()
    {
        if (flashOverlay == null) return;

        if (flashingCoroutine != null)
        {
            StopCoroutine(flashingCoroutine);
            flashingCoroutine = null;
        }
        flashOverlay.gameObject.SetActive(false);
    }

    private IEnumerator FlashCoroutine()
    {
        flashOverlay.gameObject.SetActive(true);
        while (true)
        {
            flashOverlay.enabled = !flashOverlay.enabled;
            yield return new WaitForSeconds(1.0f);
        }
    }
}