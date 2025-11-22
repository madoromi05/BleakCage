using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class DeckEditManager : MonoBehaviour
{
    public static DeckEditManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject deckEditPanel; // デッキ編集画面全体の親
    [SerializeField] private Transform cardInventoryContent; // カード一覧を表示するScrollViewのContent
    [SerializeField] private Transform characterListContent; // キャラクターを表示するエリア

    [Header("Prefabs")]
    [SerializeField] private GameObject cardRowPrefab; // 横長リスト形式のカードUI
    [SerializeField] private GameObject characterSlotPrefab; // キャラクター表示用UI
    [SerializeField] private GameObject weaponUiPrefab;

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
        // 起動したらデッキロード画面を開く（テスト用）
        deckEditPanel.SetActive(true);
        DraggableItem.IsEditingMode = true;
        LoadDataAndBuildUI();
    }
    // UIボタンなどから呼ばれる
    public void OpenDeckEdit()
    {
        deckEditPanel.SetActive(true);
        DraggableItem.IsEditingMode = true;
        LoadDataAndBuildUI();
    }

    // UIボタンなどから呼ばれる
    public void CloseAndSave()
    {
        DataManager.SaveData(currentProfile, ProfileFileName);
        DraggableItem.IsEditingMode = false;
        deckEditPanel.SetActive(false);
    }

    private void LoadDataAndBuildUI()
    {
        currentProfile = DataManager.LoadData<PlayerProfile>(ProfileFileName);

        ClearUI(characterListContent);
        ClearUI(cardInventoryContent);

        // 1. キャラクターと装備の表示
        foreach (var charData in currentProfile.BattleCharacters)
        {
            // キャラ枠生成
            GameObject charObj = Instantiate(characterSlotPrefab, characterListContent);

            // ここでキャラ名の設定などができます
            // var charNameText = charObj.transform.Find("NameText").GetComponent<Text>();
            // charNameText.text = "Character " + charData.CharacterId;

            // A. 直差しカードの表示 (例: CardListRoot という名前の親オブジェクトがプレハブにある想定)
            Transform cardRoot = FindChildRecursive(charObj.transform, "CardListRoot");
            if (cardRoot != null)
            {
                // 親スロット情報の設定 (DropSlotがついている想定)
                var slot = cardRoot.GetComponent<DropSlot>();
                if (slot != null) slot.OwnerInstanceId = charData.InstanceId;

                foreach (var cardData in charData.EquippedCards)
                {
                    CreateCardUI(cardRoot, cardData);
                }
            }

            // B. 武器とその中身の表示
            Transform weaponRoot = FindChildRecursive(charObj.transform, "WeaponListRoot");
            if (weaponRoot != null)
            {
                foreach (var weaponData in charData.EquippedWeapons)
                {
                    // 武器生成 (武器用のプレハブ/Controllerがある場合はそちらを使用)
                    // 今回は簡易的に武器も似たUIとして生成するか、あるいはWeapon専用処理を書く
                    // GameObject weaponObj = Instantiate(weaponUiPrefab, weaponRoot);
                    // ... 武器の表示処理 ...

                    // 武器の中のスロット
                    // Transform weaponSlot = weaponObj.transform.Find("SlotRoot");
                    // foreach(var slottedCard in weaponData.SlottedCards)
                    // {
                    //     CreateCardUI(weaponSlot, slottedCard);
                    // }
                }
            }
        }

        // 2. 所持カード（インベントリ）の表示
        // 本来は「全所持カード - 装備済みカード」のリストを渡すが、ここでは仮に生成
        // List<CardData> inventoryCards = ...;
        // foreach(var card in inventoryCards) { CreateCardUI(cardInventoryContent, card); }

        // テスト用: ID 1〜5のカードを表示してみる
        for (int i = 1; i <= 5; i++)
        {
            CardData dummyData = new CardData { InstanceId = System.Guid.NewGuid().ToString(), CardId = i };
            CreateCardUI(cardInventoryContent, dummyData);
        }
    }

    /// <summary>
    /// ★ここがUI表示の核心部分です
    /// </summary>
    private void CreateCardUI(Transform parent, CardData data)
    {
        // 1. プレハブを生成
        GameObject uiObj = Instantiate(cardRowPrefab, parent);

        // 2. CardControllerを使って表示内容を更新
        //    ご提示の CardController, CardView, CardModelFactory がここで連携します
        CardController controller = uiObj.GetComponent<CardController>();
        if (controller != null)
        {
            // IDからモデルを読み込み
            CardModel model = cardFactory.CreateFromID(data.CardId);
            if (model != null)
            {
                // ★これでアイコン・名前・説明文などが自動セットされます
                controller.Init(model);
            }
        }

        // 3. ドラッグ＆ドロップ用の情報をセット
        DraggableItem draggable = uiObj.GetComponent<DraggableItem>();
        if (draggable != null)
        {
            draggable.InstanceId = data.InstanceId;
            draggable.DataId = data.CardId;
            draggable.Type = "Card";
        }
    }

    private void ClearUI(Transform root)
    {
        foreach (Transform child in root) Destroy(child.gameObject);
    }

    // 子要素を名前で再帰的に探すヘルパー
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

    // ドラッグ移動時のコールバック (DropSlotから呼ばれる)
    public void OnItemMoved(DraggableItem item, DropSlot targetSlot)
    {
        // データ更新処理...
        Debug.Log($"Moved: {item.DataId} to {targetSlot.name}");
    }
}