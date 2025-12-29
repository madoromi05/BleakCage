using UnityEngine;
using System.Collections;
using UnityEngine.UI;


/// <summary>
/// キャラクターのステータス表示UI全体を管理する
/// </summary>
public class PlayerStatusUIController : MonoBehaviour
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
    [SerializeField] private Transform statusIconRowContainer;  // アイコンを並べる親
    [SerializeField] private StatusIconUI statusIconPrefab;     // プレハブ
    [SerializeField] private StatusIconDatabase iconDatabase;   // データベース

    private Color _originalBackgroundColor;
    private Coroutine _hpAnimationCoroutine;
    private PlayerRuntime _playerRuntime;
    private float _maxHp;

    public int PlayerId { get; private set; }

    private void Awake()
    {
        // 起動時に元の背景色を記憶しておく
        if (background != null)
        {
            _originalBackgroundColor = background.color;
        }
    }

    private void OnDisable()
    {
        // 非アクティブ化されるとコルーチンが止められず事故りやすいので明示停止
        if (_hpAnimationCoroutine != null)
        {
            StopCoroutine(_hpAnimationCoroutine);
            _hpAnimationCoroutine = null;
        }
    }
    /// <summary>
    /// Playerのデータを使ってUIを初期設定する
    /// </summary>
    public void SetPlayerStatus(PlayerRuntime playerRuntime)
    {
        _playerRuntime = playerRuntime;
        if (_playerRuntime == null || _playerRuntime.PlayerModel == null)
        {
            Debug.LogError("SetPlayerStatus: playerRuntime or PlayerModel is null.", this);
            return;
        }

        PlayerModel model = _playerRuntime.PlayerModel;

        PlayerId = model.PlayerID;

        if (nameText != null) nameText.text = model.PlayerName;
        if (levelText != null) levelText.text = model.PlayerLevel.ToString();
        if (attackText != null) attackText.text = (model.PlayerLevel * 10).ToString();
        if (defenseText != null) defenseText.text = model.PlayerDefensePower.ToString();

        if (characterIcon != null)
        {
            if (model.PlayerIcon != null)
            {
                characterIcon.sprite = model.PlayerIcon;
            }
            else
            {
                Debug.LogWarning($"PlayerModel '{model.PlayerName}' にUI用のPlayerIconが設定されていません。", this);
            }
        }

        _maxHp = model.PlayerHP;

        if (hpSlider != null)
        {
            hpSlider.minValue = 0f;
            hpSlider.maxValue = _maxHp;

            float clamped = Mathf.Clamp(_playerRuntime.CurrentHP, 0f, _maxHp);
            hpSlider.value = clamped;
        }
    }

    /// <summary>
    /// HPバーの表示を更新する（HP実数で扱う）
    /// </summary>
    public void UpdateHp(float currentHp)
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy) return;
        if (hpSlider == null) return;

        float targetHp = Mathf.Clamp(currentHp, 0f, _maxHp);

        if (_hpAnimationCoroutine != null)
        {
            StopCoroutine(_hpAnimationCoroutine);
            _hpAnimationCoroutine = null;
        }

        _hpAnimationCoroutine = StartCoroutine(AnimateHpBarCoroutine(targetHp));
    }

    public void UpdateStatusIcons(StatusEffectHandler statusHandler)
    {
        if (statusIconRowContainer == null) return;

        foreach (Transform child in statusIconRowContainer)
        {
            Destroy(child.gameObject);
        }

        if (statusHandler == null || iconDatabase == null || statusIconPrefab == null) return;

        foreach (var effect in statusHandler.ActiveStatusEffects)
        {
            Sprite iconSprite = iconDatabase.GetIcon(effect.Type);
            if (iconSprite == null) continue;

            StatusIconUI newIcon = Instantiate(statusIconPrefab, statusIconRowContainer);
            newIcon.Setup(iconSprite, effect.StackCount);
        }
    }

    private IEnumerator AnimateHpBarCoroutine(float targetHp)
    {
        float startHp = hpSlider.value;
        float elapsed = 0f;

        // durationが0以下なら即反映
        if (hpAnimationDuration <= 0f)
        {
            hpSlider.value = targetHp;
            _hpAnimationCoroutine = null;
            yield break;
        }

        while (elapsed < hpAnimationDuration)
        {
            // 途中でUIが消されたら安全に終了
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / hpAnimationDuration);
            hpSlider.value = Mathf.Lerp(startHp, targetHp, t);
            yield return null;
        }

        hpSlider.value = targetHp;
        _hpAnimationCoroutine = null;
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
            background.color = _originalBackgroundColor;
        }
    }

    /// <summary>
    /// このUIが管理している PlayerRuntime データを返す
    /// </summary>
    public PlayerRuntime GetPlayerRuntime()
    {
        return this._playerRuntime;
    }
}