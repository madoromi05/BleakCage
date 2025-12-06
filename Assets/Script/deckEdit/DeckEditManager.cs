using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class DeckEditManager : MonoBehaviour
{
    public static DeckEditManager Instance { get; private set; }

    [Header("UI Areas")]
    [SerializeField] private GameObject deckEditPanel;
    [SerializeField] private Transform leftSidePanelRoot;  // 左側パネルの親
    [SerializeField] private Text characterNameText;       // 左側一番上のキャラ名表示用テキスト
    [SerializeField] private Transform rightSidePanelRoot; // 右側パネルの親

    [Header("Prefabs")]
    [SerializeField] private GameObject weaponNameTextPrefab; // 新規: 左側の武器名表示用
    [SerializeField] private GameObject cardRowPanelPrefab;   // 新規: 右側のカード1行分の枠
    [SerializeField] private GameObject cardPrefab;           // カード単体

    private PlayerProfile currentProfile;
    private const string ProfileFileName = "player_profile.json";
    private CardModelFactory cardFactory;
    private WeaponModelFactory weaponFactory;
    private PlayerModelFactory playerFactory;
    private CharacterData selectedCharacter;
    private PlayerDataLoader dataLoader;
    private void Awake()
    {
        Instance = this;
        cardFactory = new CardModelFactory();
        weaponFactory = new WeaponModelFactory();
        playerFactory = new PlayerModelFactory();
        dataLoader = new PlayerDataLoader();
    }

    private void Start()
    {
        // 1. まずデータをロードしてみる
        currentProfile = DataManager.LoadData<PlayerProfile>(ProfileFileName);

        // 2. データがなければ、PlayerDataLoaderを使って生成・保存・再ロードする
        if (currentProfile == null || currentProfile.BattleCharacters == null || currentProfile.BattleCharacters.Count == 0)
        {
            Debug.Log("セーブデータが見つからないため、PlayerDataLoaderを使って新規作成します。");
            dataLoader.LoadPlayerPartyAndCards();

            // 保存されたファイルを読み直す
            currentProfile = DataManager.LoadData<PlayerProfile>(ProfileFileName);
        }

        if (currentProfile != null && currentProfile.BattleCharacters.Count > 0)
        {
            OpenDeckView(currentProfile.BattleCharacters[0]);
        }
    }

    public void OpenDeckView(CharacterData charData)
    {
        selectedCharacter = charData;
        deckEditPanel.SetActive(true);
        BuildLayout();
    }
    private void BuildLayout()
    {
        if (selectedCharacter == null) return;
        ClearUI(leftSidePanelRoot, characterNameText.gameObject);
        ClearUI(rightSidePanelRoot);

        //  左側：キャラ名の表示
        PlayerModel charModel = playerFactory.CreateFromId(selectedCharacter.CharacterId);
        if (charModel != null && characterNameText != null)
        {
            characterNameText.text = charModel.PlayerName;
        }

        // 右側：1行目（キャラカード）の作成
        CreateCardRow(selectedCharacter.EquippedCards);

        // 武器ごとの処理（左側に名前、右側にカード行）
        if (selectedCharacter.EquippedWeapons != null)
        {
            foreach (var weaponData in selectedCharacter.EquippedWeapons)
            {
                // 左側：武器名の追加
                WeaponModel weaponModel = weaponFactory.CreateFromId(weaponData.WeaponId);

                if (weaponModel != null)
                {
                    GameObject nameObj = Instantiate(weaponNameTextPrefab, leftSidePanelRoot);
                    Text nameText = nameObj.GetComponentInChildren<Text>();
                    if (nameText != null)
                    {
                        nameText.text = weaponModel.Name;
                    }
                    Image iconImage = nameObj.GetComponentInChildren<Image>();

                    if (iconImage != null)
                    {
                        if (weaponModel.Icon != null)
                        {
                            iconImage.sprite = weaponModel.Icon;
                            iconImage.enabled = true; // 画像を表示
                        }
                        else
                        {
                            iconImage.enabled = false;
                        }
                    }
                }

                // 右側：この武器のカード行を作成
                CreateCardRow(weaponData.SlottedCards);
            }
        }
    }

    // 右側にカード1行分を作成するヘルパーメソッド
    private void CreateCardRow(List<CardData> cardList)
    {
        // 行の枠（Horizontal Layout Group付き）を生成
        GameObject rowObj = Instantiate(cardRowPanelPrefab, rightSidePanelRoot);

        // その中にカードを生成して並べる
        if (cardList != null)
        {
            foreach (var cardData in cardList)
            {
                CreateCardUI(rowObj.transform, cardData.CardId);
            }
        }
    }

    // カード単体を生成する共通メソッド（変更なし）
    private void CreateCardUI(Transform parent, int cardId)
    {
        CardModel model = cardFactory.CreateFromID(cardId);
        if (model == null) return;

        GameObject uiObj = Instantiate(cardPrefab, parent);

        CardController controller = uiObj.GetComponent<CardController>();
        if (controller != null) controller.Init(model);

        CardView view = uiObj.GetComponent<CardView>();
        if (view != null) view.Show(model);
    }

    // 指定したルート以下の子要素を削除する（例外指定付き）
    private void ClearUI(Transform root, GameObject exclude = null)
    {
        foreach (Transform child in root)
        {
            if (exclude != null && child.gameObject == exclude) continue;
            Destroy(child.gameObject);
        }
    }
}