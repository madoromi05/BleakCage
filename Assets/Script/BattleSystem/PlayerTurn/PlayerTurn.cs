/// <summary>
/// プレイヤーのカード選択、手札管理
/// </summary>
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI; // UI操作のために追加

public class PlayerTurn : MonoBehaviour
{
    [SerializeField] private CardController _cardPrefab;
    [SerializeField] private Transform _playerHandTransform;
    [SerializeField] private BattleInputReader _inputReader;
    [SerializeField] private GuardGaugeSystem _guardGaugeSystem;
    [SerializeField] private DamageCalculator _damageCalculator;
    [SerializeField] private BattleCardPresenter _battleCardPresenter;
    [SerializeField] private float _cardSelectionYOffset = 30.0f;
    [SerializeField] private GameObject _enterUI;
    [SerializeField] private GameObject _key1UI;
    [SerializeField] private GameObject _key2UI;
    [SerializeField] private GameObject _key3UI;

    public event System.Action OnTurnFinished;
    public event System.Action<int> OnCardSelected;

    public bool IsTurnFinished;

    public event System.Action<int, bool> OnCardSelectedForTutorial;  // カード選択時
    public event System.Action OnConfirmSelectionForTutorial;         // 選択確定時

    private BattleCardDeck _battleDeck;
    private CardModelFactory _cardModelFactory;
    private CardRuntime _cardRuntime;
    private EnemyStatusUIController _singleEnemyStatusUIController; // 使用箇所がないようですが命名規則を適用
    private List<PlayerStatusUIController> _playerStatusUIControllers;

    private List<CardController> _handCardControllers = new();                          // 手札のカード表示
    private List<CardRuntime> _handCards = new();                                       // 手札のカードdata
    private List<CardRuntime> _selectedCardsThisTurn = new List<CardRuntime>();         // 選択されたカードのIDを保持
    private Dictionary<int, List<EnemyModel>> _playerTargetSelections;
    private List<System.Guid> _excludedCardInstancesThisTurn = new List<System.Guid>(); // 破棄されたカードのIDを保持
    private List<EnemyStatusUIController> _enemyStatusUIControllers;
    private Dictionary<EnemyModel, EnemyController> _enemyControllers;
    private List<EnemyRuntime> _allEnemyRuntimes;
    private List<Vector3> _cardInitialPositions = new List<Vector3>();

    private bool _isCounterTurn = false;
    private System.Action _onCounterActionFinishedCallback;
    private bool[] _isCardSelected = new bool[3];       // 各カード（3枚）が選択されているかどうか
    private bool _inputEnabled = false;                 // ターン中全体の入力フラグ
    private bool _isInputLocked = false;                // 入力受付処理中に他の入力を受け取らない
    private bool _isTutorialMode = false;
    private float _lastInputTime = 0f;
    private float _inputCooldown = 0.1f;
    private PlayerActionExecutor _actionExecutor;
    private List<PlayerRuntime> _allPlayers;

    private AudioSource _audioSource;

    private Dictionary<int, GameObject> _keyUI;
    private List<GameObject> _activeKeyUIObjects = new List<GameObject>();

    private void Awake()
    {
        _inputReader.CardSelectEvent += OnCardSelect;
        _inputReader.DisCardEvent += OnConfirmSelectionAndRedraw;

        _cardModelFactory = new CardModelFactory();
        _actionExecutor = new PlayerActionExecutor(this);
        _audioSource = GetComponent<AudioSource>();
        if (_battleCardPresenter != null)
        {
            _battleCardPresenter.Setup(_cardModelFactory, _playerHandTransform, _cardPrefab);
        }
        else
        {
            DebugCostom.LogError("BattleCardPresenter がインスペクターで設定されていません！");
        }

        _keyUI = new Dictionary<int, GameObject>();
        _keyUI[0] = _key1UI;
        _keyUI[1] = _key2UI;
        _keyUI[2] = _key3UI;
        if (_enterUI != null) _enterUI.SetActive(false);
    }

    public void Setup(Dictionary<int, List<EnemyModel>> playerSelections,
                      List<PlayerRuntime> allPlayers,
                      BattleCardDeck battleDeck,
                      List<EnemyStatusUIController> enemyUIControllers,
                      List<PlayerStatusUIController> playerUIControllers,
                      Dictionary<EnemyModel, EnemyController> enemyControllers,
                      List<EnemyRuntime> enemyRuntimes)
    {
        this._playerTargetSelections = playerSelections;
        this._allPlayers = allPlayers;
        this._battleDeck = battleDeck;
        this._enemyStatusUIControllers = enemyUIControllers;
        this._playerStatusUIControllers = playerUIControllers;
        this._enemyControllers = enemyControllers;
        this._allEnemyRuntimes = enemyRuntimes;
    }

    public void SetTutorialMode(bool mode)
    {
        _isTutorialMode = mode;
    }

    public void StartPlayerTurn()
    {
        _isCounterTurn = false;
        IsTurnFinished = false;
        _selectedCardsThisTurn.Clear();
        _excludedCardInstancesThisTurn.Clear();
        _cardInitialPositions.Clear();
        _battleDeck.ResetBattleDeck(_battleDeck.battleCardDeck);
        _inputEnabled = true;

        if (_enterUI != null) _enterUI.SetActive(false);

        DrawHandCards();
    }

    // ターン終わりにCardの効果処理
    public void FinishPlayerTurn()
    {
        if (IsTurnFinished) return;
        IsTurnFinished = true;
        _inputEnabled = false;

        // 手札のカード表示をdestory
        foreach (var contCard in _handCardControllers)
        {
            if (contCard != null)
            {
                Destroy(contCard.gameObject);
            }
        }
        _handCardControllers.Clear();

        // --- キーガイドUIの削除 ---
        foreach (var keyObj in _activeKeyUIObjects)
        {
            if (keyObj != null) Destroy(keyObj);
        }
        _activeKeyUIObjects.Clear();

        StartCoroutine(_actionExecutor.ExecuteActions(
           _selectedCardsThisTurn,
           _playerTargetSelections,
           _enemyStatusUIControllers,
           _playerStatusUIControllers,
           _enemyControllers,
           _allEnemyRuntimes,
           _damageCalculator,
           _battleCardPresenter.ShowCard,
           _battleCardPresenter.HideCard,
           () => OnTurnFinished?.Invoke()
       ));
    }

    /// <summary>
    /// デッキから手札を3枚引き、表示する
    /// </summary>
    public void DrawHandCards()
    {
        foreach (var contCard in _handCardControllers)
        {
            if (contCard != null) Destroy(contCard.gameObject);
        }

        _handCardControllers.Clear();

        // --- キーガイドUIのリセット ---
        foreach (var keyObj in _activeKeyUIObjects)
        {
            if (keyObj != null) Destroy(keyObj);
        }
        _activeKeyUIObjects.Clear();

        _handCards.Clear();
        _isCardSelected = new bool[3];
        int drawnCardCount = 0;

        //三枚提示
        for (int i = 0; i < 3; i++)
        {
            if (_battleDeck.TryDrawCard(out CardRuntime drawnCard))
            {
                var cardObject = Instantiate(_cardPrefab, _playerHandTransform, false);
                CardModel cardModel = _cardModelFactory.CreateFromID(drawnCard.ID);
                float basePower = drawnCard.weaponRuntime.ParentPlayer.Level + drawnCard.weaponRuntime.attackPower;
                cardObject.Init(cardModel, basePower);
                _handCards.Add(drawnCard);
                _handCardControllers.Add(cardObject);
                drawnCardCount++;

                // --- キーガイドUIの生成と配置 ---
                if (_keyUI.ContainsKey(i))
                {
                    GameObject keyObj = Instantiate(_keyUI[i], cardObject.transform, false);
                    keyObj.SetActive(true);

                    RectTransform rt = keyObj.GetComponent<RectTransform>();
                    // 位置調整 (カードの上部に表示するよう設定)
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(0, 190);
                    _activeKeyUIObjects.Add(keyObj);
                }
            }
        }

        if (drawnCardCount == 0)
        {
            if (!_isCounterTurn)
            {
                FinishPlayerTurn();
            }
            return;
        }

        Canvas canvas = _playerHandTransform.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Canvas.ForceUpdateCanvases();
        }
        else
        {
            var layoutGroup = _playerHandTransform.GetComponent<UnityEngine.UI.LayoutGroup>();
            if (layoutGroup != null)
            {
                layoutGroup.CalculateLayoutInputHorizontal();
                layoutGroup.CalculateLayoutInputVertical();
                layoutGroup.SetLayoutHorizontal();
                layoutGroup.SetLayoutVertical();
            }
        }
        _cardInitialPositions.Clear();
        for (int i = 0; i < _handCardControllers.Count; i++)
        {
            _cardInitialPositions.Add(Vector3.zero);
        }
        UpdateAllCardVisuals();
    }

    /// <summary>
    /// 1,2,3ボタンでカードを選択
    /// </summary>
    private void OnCardSelect(int inputNumber)
    {
        if (!_inputEnabled)
        {
            DebugCostom.LogWarning($"入力が無効です (inputEnabled=false)");
            return;
        }
        if (_isInputLocked)
        {
            DebugCostom.LogWarning($"入力がロックされています (isInputLocked=true)");
            return;
        }
        if (Time.time - _lastInputTime < _inputCooldown) return;

        _lastInputTime = Time.time;
        _isInputLocked = true;
        SelectCard(inputNumber);
        OnCardSelected?.Invoke(inputNumber);
        SoundManager.Instance.PlaySE(SEType.Check);
        if (_isTutorialMode)
        {
            OnCardSelectedForTutorial?.Invoke(inputNumber, _isCardSelected[inputNumber]);
        }
        _isInputLocked = false;
    }

    /// <summary>
    /// CardNumber番目のカードを選択するメソッド
    /// </summary>
    private void SelectCard(int inputNumber)
    {
        if (inputNumber < 0 || inputNumber >= _isCardSelected.Length ||
            inputNumber >= _handCardControllers.Count || inputNumber >= _cardInitialPositions.Count)
        {
            DebugCostom.Log($"無効なカード番号: {inputNumber}");
            return;
        }

        if (_isCardSelected[inputNumber])
        {
            _isCardSelected[inputNumber] = false;
            if (_audioSource != null) SoundManager.Instance.PlaySE(SEType.checkedSkillCard);
        }
        else
        {
            // 選択制限チェック
            if (!CanSelectCard())
            {
                DebugCostom.Log("選択可能なカードは2枚までです。");
                return;
            }

            _isCardSelected[inputNumber] = true;
        }

        UpdateAllCardVisuals();
    }

    /// <summary>
    /// カードを選択できるかチェック
    /// </summary>
    private bool CanSelectCard()
    {
        int selectedCount = _isCardSelected.Count(x => x);
        return selectedCount < 2;
    }

    /// <summary>
    /// タイマーと入力管理
    /// </summary>
    private void OnConfirmSelectionAndRedraw()
    {
        if (!_inputEnabled || _isInputLocked) return;

        if (_isTutorialMode)
        {
            OnConfirmSelectionForTutorial?.Invoke();
        }
        else
        {
            if (Time.time - _lastInputTime < _inputCooldown) return;
            _lastInputTime = Time.time;
        }

        _isInputLocked = true;
        ConfirmSelectionAndRedraw();
        _isInputLocked = false;
    }

    /// <summary>
    /// カードの選択を確定し、手札を再抽選する
    /// </summary>
    private void ConfirmSelectionAndRedraw()
    {
        if (!_inputEnabled) return;

        // 選択されているカードがない場合は何もしない
        int selectedCount = _isCardSelected.Count(x => x);
        if (selectedCount == 0)
        {
            DebugCostom.Log("1枚以上カードを選択してください。");
            return;
        }

        List<CardRuntime> cardsToExecute = new List<CardRuntime>();

        for (int i = 0; i < _isCardSelected.Length; i++)
        {
            if (i >= _handCards.Count) continue;

            CardRuntime cardInstance = _handCards[i];
            if (_isCardSelected[i])
            {
                if (_isCounterTurn)
                {
                    cardsToExecute.Add(cardInstance);
                }
                else
                {
                    _selectedCardsThisTurn.Add(cardInstance);
                }
            }
            else
            {
                _excludedCardInstancesThisTurn.Add(cardInstance.InstanceID);
            }
        }

        //選択状態をリセット
        for (int i = 0; i < _isCardSelected.Length; i++)
        {
            _isCardSelected[i] = false;
        }

        if (_isCounterTurn)
        {
            _isCounterTurn = false;
            _inputEnabled = false;

            // 手札のカード表示をdestory
            foreach (var contCard in _handCardControllers)
            {
                if (contCard != null) Destroy(contCard.gameObject);
            }
            _handCardControllers.Clear();

            // UI破棄
            foreach (var keyObj in _activeKeyUIObjects)
            {
                if (keyObj != null) Destroy(keyObj);
            }
            _activeKeyUIObjects.Clear();

            _handCards.Clear();

            StartCoroutine(_actionExecutor.ExecuteActions(
                cardsToExecute,
                _playerTargetSelections,
                _enemyStatusUIControllers,
                _playerStatusUIControllers,
                _enemyControllers,
                _allEnemyRuntimes,
                _damageCalculator,
                _battleCardPresenter.ShowCard,
                _battleCardPresenter.HideCard,
                () =>
                {
                    _onCounterActionFinishedCallback?.Invoke();
                    _onCounterActionFinishedCallback = null;
                    OnTurnFinished?.Invoke();
                }
            ));
        }
        else
        {
            if (!_isTutorialMode)
            {
                DrawHandCards();
            }
            else
            {
                UpdateAllCardVisuals();
            }
        }
    }

    /// <summary>
    /// カウンター成功時のエクストラアクション（カード1回提示）を開始する
    /// </summary>
    /// <param name="onCounterActionFinished">このアクションが完了したときに呼ばれるコールバック</param>
    public void StartCounterAction(System.Action onCounterActionFinished)
    {
        Debug.Log("カウンターアクションを開始します");
        _isCounterTurn = true;    // カウンターモードをON
        IsTurnFinished = false;  // ターン実行中
        _inputEnabled = true;
        _isInputLocked = false;

        // このコールバックを保存
        this._onCounterActionFinishedCallback = onCounterActionFinished;

        // 3枚引いて表示
        DrawHandCards();
    }

    /// <summary>
    /// 手札のすべてのカードのビジュアル（選択時のYオフセット、選択可否の見た目）を更新する
    /// </summary>
    private void UpdateAllCardVisuals()
    {
        int selectedCount = _isCardSelected.Count(x => x);

        // 2枚（最大数）選択されているか
        bool maxCardsSelected = (selectedCount >= 2);

        // --- Enter UIの表示制御 ---
        if (_enterUI != null)
        {
            _enterUI.SetActive(selectedCount > 0);
        }

        for (int i = 0; i < _handCardControllers.Count; i++)
        {
            if (_handCardControllers[i] == null) continue;

            CardController cardObject = _handCardControllers[i];

            // --- キーガイドの表示制御 ---
            if (i < _activeKeyUIObjects.Count && _activeKeyUIObjects[i] != null)
            {
                _activeKeyUIObjects[i].SetActive(!_isCardSelected[i]);
            }

            bool shouldBeInteractable = true;
            if (!_isCardSelected[i] && maxCardsSelected)
            {
                shouldBeInteractable = false;
            }

            cardObject.SetInteractable(shouldBeInteractable);

            Transform visualRoot = cardObject.transform.Find("VisualRoot");
            if (visualRoot == null)
            {
                Debug.LogError("CardController プレハブに 'VisualRoot' という名前の子オブジェクトが見つかりません！");
                continue;
            }

            if (_isCardSelected[i])
            {
                visualRoot.localPosition = new Vector3(0, _cardSelectionYOffset, 0);
            }
            else
            {
                visualRoot.localPosition = Vector3.zero;
            }
        }
    }
}