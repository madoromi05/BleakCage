using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyStatusUIController : MonoBehaviour
{
    [SerializeField] private Image characterIcon;
    [SerializeField] private Text nameText;
    [SerializeField] private Text levelText;
    [SerializeField] private Text attackText;
    [SerializeField] private Text defenseText;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private float hpAnimationDuration = 0.5f;
    [SerializeField] private Image background;

    [Header("バフとデバフ用")]
    [SerializeField] private Transform statusIconRowContainer;  // アイコンを並べる親オブジェクト
    [SerializeField] private StatusIconUI statusIconPrefab;     // 作成したプレハブ
    [SerializeField] private StatusIconDatabase iconDatabase;   // 作成したデータベース

    private Color originalBackgroundColor;
    private Coroutine hpAnimationCoroutine;
    private EnemyModel enemyModel;
    private float maxHP;

    private void Awake()
    {
        // 起動時に元の背景色を記憶しておく
        if (background != null)
        {
            originalBackgroundColor = background.color;
        }
    }

    /// <summary>
    /// Enemyのデータを使ってUIを初期設定する
    /// </summary>
    public void SetEnemyStatus(EnemyModel enemy)
    {
        this.enemyModel = enemy;
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

        this.maxHP = enemy.EnemyHP;
        hpSlider.maxValue = enemy.EnemyHP;
        hpSlider.value = enemy.EnemyHP;
    }

    public void UpdateStatusIcons(StatusEffectHandler statusHandler)
    {
        // 既存のアイコンをすべて削除
        foreach (Transform child in statusIconRowContainer)
        {
            Destroy(child.gameObject);
        }

        if (statusHandler == null) return;

        // 現在有効なステータス効果のリストを取得してループ
        foreach (var effect in statusHandler.ActiveStatusEffects)
        {
            Sprite iconSprite = iconDatabase.GetIcon(effect.Type);

            if (iconSprite != null)
            {
                // プレハブを生成
                StatusIconUI newIcon = Instantiate(statusIconPrefab, statusIconRowContainer);
                newIcon.Setup(iconSprite, effect.StackCount);
            }
        }
    }

    /// <summary>
    /// このUIが担当しているEnemyModelを返す
    /// </summary>
    public EnemyModel GetEnemyModel()
    {
        return this.enemyModel;
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

        hpAnimationCoroutine = StartCoroutine(AnimateHPBarCoroutine(currentHP));
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
