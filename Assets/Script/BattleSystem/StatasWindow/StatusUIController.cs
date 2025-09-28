using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// キャラクターのステータス表示UI全体を管理する
/// </summary>
public class StatusUIController : MonoBehaviour
{
    [SerializeField] private Image characterIcon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI defenseText;
    [SerializeField] private Slider hpSlider;

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
    /// Enemyのデータを使ってUIを初期設定する
    /// </summary>
    public void SetEnemyStatus(EnemyModel enemy)
    {
        nameText.text = enemy.EnemyName;
        attackText.text = enemy.EnemyAttackPower.ToString();
        defenseText.text = enemy.EnemyDefensePower.ToString();

        // 敵にはレベルがないので、レベル表示を非表示にする
        if (levelText != null)
        {
            levelText.gameObject.SetActive(false);
        }

        if (enemy.EnemySprite != null)
        {
            characterIcon.sprite = enemy.EnemySprite;
        }

        hpSlider.maxValue = enemy.EnemyHP;
        hpSlider.value = enemy.EnemyHP;
    }

    /// <summary>
    /// HPバーの表示を更新する
    /// </summary>
    public void UpdateHP(float currentHP)
    {
        hpSlider.value = currentHP;
    }
}