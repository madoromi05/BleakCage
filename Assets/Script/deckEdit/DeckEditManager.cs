using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeckEditManager : MonoBehaviour
{
    public static DeckEditManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject deckEditPanel; // パネル全体
    [SerializeField] private Transform characterListContent; // キャラクターを表示するエリア
    [SerializeField] private Transform cardInventoryContent;

    [Header("Prefabs")]
    [SerializeField] private GameObject cardRowPrefab; // カードUIプレハブ
    [SerializeField] private GameObject characterSlotPrefab; // キャラクター表示用UI

    private PlayerProfile currentProfile;
    private const string ProfileFileName = "player_profile.json";
    private CardModelFactory cardFactory;

    private void Awake()
    {
        Instance = this;
        cardFactory = new CardModelFactory();
    }

    private void Start()
    {
        // 起動時にデータを読み込んで表示
        OpenDeckView();
    }

    public void OpenDeckView()
    {
        deckEditPanel.SetActive(true);
        LoadDataAndBuildUI();
    }

    public void ClosePanel()
    {
        deckEditPanel.SetActive(false);
    }

    private void LoadDataAndBuildUI()
    {
        currentProfile = DataManager.LoadData<PlayerProfile>(ProfileFileName);

        // 表示エリアをクリア
        ClearUI(characterListContent);
        if (cardInventoryContent != null) ClearUI(cardInventoryContent);

        // キャラクターごとに装備カードを表示
        foreach (var charData in currentProfile.BattleCharacters)
        {
            // 1. キャラクター枠を生成
            GameObject charObj = Instantiate(characterSlotPrefab, characterListContent);

            // 2. 直差しカードの表示エリアを探す
            // (プレハブ内で "CardListRoot" という名前のオブジェクトを作っておいてください)
            Transform cardRoot = FindChildRecursive(charObj.transform, "CardListRoot");

            if (cardRoot != null)
            {
                // 装備しているカードを生成して並べる
                foreach (var cardData in charData.EquippedCards)
                {
                    CreateCardUI(cardRoot, cardData);
                }
            }
        }
    }

    private void CreateCardUI(Transform parent, CardData data)
    {
        // プレハブ生成
        GameObject uiObj = Instantiate(cardRowPrefab, parent);

        // 表示内容の更新 (CardControllerを使用)
        CardController controller = uiObj.GetComponent<CardController>();
        if (controller != null)
        {
            CardModel model = cardFactory.CreateFromID(data.CardId);
            if (model != null)
            {
                controller.Init(model);
            }
        }
    }

    private void ClearUI(Transform root)
    {
        foreach (Transform child in root) Destroy(child.gameObject);
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindChildRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }
}