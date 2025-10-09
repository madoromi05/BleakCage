using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyStatusUIController : MonoBehaviour
{
    [SerializeField] private Image characterIcon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI defenseText;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Image flashOverlay;
    private Coroutine flashingCoroutine;

    private void Awake()
    {
        if (flashOverlay != null)
        {
            flashOverlay.gameObject.SetActive(false);
        }
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
            yield return new WaitForSeconds(0.25f);
        }
    }
}
