using System.Collections.Generic;
using System.Linq; // ★追加: Linqを使います
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DeckViewManager : MonoBehaviour
{
    public static DeckViewManager Instance { get; private set; }

    [Header("UI Areas")]
    [SerializeField] private GameObject deckEditPanel;
    [SerializeField] private Transform leftSidePanelRoot;

    [SerializeField] private Text characterNameText;
    [SerializeField] private Image characterIconImage;

    [SerializeField] private Transform rightSidePanelRoot;

    [Header("Buttons")]
    [SerializeField] private Button battleStartButton;
    [SerializeField] private Button prevCharButton;
    [SerializeField] private Button nextCharButton;

    [Header("Prefabs")]
    [SerializeField] private GameObject weaponNameTextPrefab;
    [SerializeField] private GameObject cardRowPanelPrefab;
    [SerializeField] private GameObject cardPrefab;

    [Header("Data Settings")]
    [SerializeField] private List<StagePlayerSetup> stagePlayerPresets;

    [Header("Debug Settings (Editor Only)")]
    [SerializeField] private bool useDebugSettings = false;
    [SerializeField] private int debugStageID = 0;

    // データ管理用
    private DeckSetupRepository currentDeckData;
    private int currentPlayerIndex = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 1. ボタンイベントの登録
        if (battleStartButton != null)
            battleStartButton.onClick.AddListener(OnClickBattleStart);

        if (prevCharButton != null)
            prevCharButton.onClick.AddListener(OnClickPrevChar);

        if (nextCharButton != null)
            nextCharButton.onClick.AddListener(OnClickNextChar);

        // 2. ステージIDの決定
        int targetStageID = 0;
        if (StageManager.SelectedStageID != -1)
        {
            targetStageID = StageManager.SelectedStageID;
        }
        else if (Application.isEditor && useDebugSettings)
        {
            targetStageID = debugStageID;
            Debug.Log($"<color=yellow>【Debug】DeckView: DebugStageID {targetStageID} を使用して表示します</color>");
        }

        // 3. データロード
        LoadDeckData(targetStageID);

        // 4. 初期表示
        if (currentDeckData != null && currentDeckData.Party.Count > 0)
        {
            currentPlayerIndex = 0;
            OpenDeckView(currentPlayerIndex);
        }
        else
        {
            // データがない場合はUIを隠すか、エラー表示
            deckEditPanel.SetActive(false);
        }

        UpdateButtonState();
    }

    private void LoadDeckData(int stageID)
    {
        if (stagePlayerPresets == null || stagePlayerPresets.Count == 0) return;

        if (stageID < 0 || stageID >= stagePlayerPresets.Count)
        {
            stageID = (stagePlayerPresets.Count > 1) ? 1 : 0;
        }

        StagePlayerSetup targetPreset = stagePlayerPresets[stageID];
        PlayerDataLoader loader = new PlayerDataLoader();
        currentDeckData = loader.LoadFromPreset(targetPreset);
    }

    public void OpenDeckView(int index)
    {
        if (currentDeckData == null || currentDeckData.Party.Count == 0) return;

        // 安全策：インデックスを範囲内に収める
        index = Mathf.Clamp(index, 0, currentDeckData.Party.Count - 1);

        currentPlayerIndex = index;
        PlayerRuntime player = currentDeckData.Party[currentPlayerIndex];

        deckEditPanel.SetActive(true);
        BuildLayout(player);
        UpdateButtonState();
    }

    private void BuildLayout(PlayerRuntime player)
    {
        if (player == null) return;

        // --- 1. 左側(武器名エリア)クリア ---
        foreach (Transform child in leftSidePanelRoot)
        {
            if (characterNameText != null && child.gameObject == characterNameText.gameObject) continue;
            if (characterIconImage != null && child.gameObject == characterIconImage.gameObject) continue;
            Destroy(child.gameObject);
        }
        // --- 右側(カードエリア)クリア ---
        ClearUI(rightSidePanelRoot);

        // --- 2. キャラ基本情報 ---
        if (characterNameText != null) characterNameText.text = player.PlayerModel.PlayerName;
        if (characterIconImage != null && player.PlayerModel.PlayerIcon != null)
        {
            characterIconImage.sprite = player.PlayerModel.PlayerIcon;
            characterIconImage.enabled = true;
        }

        if (player.Weapons == null) return;

        // ★修正1: ソート処理
        // ルール: 「IDが0（キャラ）」なら優先度0、それ以外は優先度1。
        //         優先度が同じなら、「IDが大きい順」に並べる。
        // 結果: ID0(最上段) -> ID100 -> ID99 ... と並びます。
        var sortedWeapons = player.Weapons
            .OrderBy(w => w.Model.ID == 0 ? 0 : 1)
            .ThenByDescending(w => w.Model.ID)
            .ToList();

        // ★修正2: 1対1で生成 (HashSetによる重複チェックを削除)
        // 武器が3つあれば、左に名前3つ、右にカード行3つを作る。これで左右の高さがズレません。
        foreach (var weapon in sortedWeapons)
        {
            // --- A. 左側パネル: 武器名の表示 ---
            GameObject nameObj = Instantiate(weaponNameTextPrefab, leftSidePanelRoot);

            Text nameText = nameObj.GetComponentInChildren<Text>();
            if (nameText != null) nameText.text = weapon.Model.Name;

            Image weaponIcon = nameObj.GetComponentInChildren<Image>();
            if (weaponIcon != null)
            {
                if (weapon.Model.Icon != null)
                {
                    weaponIcon.sprite = weapon.Model.Icon;
                    weaponIcon.enabled = true;
                }
                else
                {
                    weaponIcon.enabled = false;
                }
            }

            // 右側パネル: カード列の生成 ---
            GameObject rowObj = Instantiate(cardRowPanelPrefab, rightSidePanelRoot);

            // カードがあれば並べる
            if (weapon.Cards != null)
            {
                foreach (var cardRuntime in weapon.Cards)
                {
                    CreateCardUI(rowObj.transform, cardRuntime.Model);
                }
            }
        }
    }

    private void CreateCardUI(Transform parent, CardModel model)
    {
        if (model == null) return;

        GameObject uiObj = Instantiate(cardPrefab, parent);

        CardView view = uiObj.GetComponent<CardView>();
        if (view != null) view.Show(model);

        CardController controller = uiObj.GetComponent<CardController>();
        if (controller != null) controller.Init(model, 1.0f);
    }

    private void ClearUI(Transform root)
    {
        foreach (Transform child in root)
        {
            Destroy(child.gameObject);
        }
    }

    // --- ボタン処理 ---

    public void OnClickNextChar()
    {
        if (currentDeckData == null) return;
        if (currentPlayerIndex < currentDeckData.Party.Count - 1)
        {
            OpenDeckView(currentPlayerIndex + 1);
        }
    }

    public void OnClickPrevChar()
    {
        if (currentDeckData == null) return;
        if (currentPlayerIndex > 0)
        {
            OpenDeckView(currentPlayerIndex - 1);
        }
    }

    public void OnClickBattleStart()
    {
        SceneManager.LoadScene("BattleScene");
    }

    private void UpdateButtonState()
    {
        if (currentDeckData == null || currentDeckData.Party.Count == 0)
        {
            if (prevCharButton != null) prevCharButton.gameObject.SetActive(false);
            if (nextCharButton != null) nextCharButton.gameObject.SetActive(false);
            return;
        }

        // 前に戻れるか？ (Index > 0)
        if (prevCharButton != null)
        {
            bool canGoPrev = (currentPlayerIndex > 0);
            prevCharButton.gameObject.SetActive(canGoPrev);
        }

        // 次に行けるか？ (Index < Count - 1)
        if (nextCharButton != null)
        {
            bool canGoNext = (currentPlayerIndex < currentDeckData.Party.Count - 1);
            nextCharButton.gameObject.SetActive(canGoNext);
        }
        LogButton(prevCharButton, "Prev");
        LogButton(nextCharButton, "Next");

    }


    private void LogButton(Button btn, string label)
    {
        if (btn == null)
        {
            Debug.Log($"[DeckView]{label} btn is NULL");
            return;
        }

        var go = btn.gameObject;
        var img = btn.GetComponent<Image>();                 // ButtonのターゲットGraphic想定
        var cg = btn.GetComponent<CanvasGroup>();            // 付いてる場合あり
        var rt = btn.GetComponent<RectTransform>();

        Debug.Log(
            $"[DeckView]{label} activeSelf={go.activeSelf} activeInHierarchy={go.activeInHierarchy} " +
            $"interactable={btn.interactable} enabled={btn.enabled} " +
            $"img={(img ? "Y" : "N")} imgEnabled={(img ? img.enabled : false)} imgAlpha={(img ? img.color.a : -1f)} " +
            $"canvasGroup={(cg ? "Y" : "N")} cgAlpha={(cg ? cg.alpha : -1f)} cgInteract={(cg ? cg.interactable : false)} " +
            $"rtPos={(rt ? rt.anchoredPosition.ToString() : "null")} rtScale={(rt ? rt.localScale.ToString() : "null")}"
        );
    }

}