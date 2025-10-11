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
    [SerializeField] private Image background;
    private Color originalBackgroundColor;

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

        nameText.text = model.PlayerName;
        levelText.text = model.PlayerLevel.ToString();
        attackText.text = (model.PlayerLevel * 10).ToString();
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