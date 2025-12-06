using System.Collections.Generic;
using UnityEngine;
using System.Linq; // OrderByを使うために必要

/// <summary>
/// Playerのパーティーとカードデータをデータベースから読み込むためのデータコンテナ
/// </summary>
public class DeckSetupRepository
{
    public List<PlayerRuntime> Party { get; }

    public DeckSetupRepository(List<PlayerRuntime> party)
    {
        Party = party;
    }
}

/// <summary>
/// PlayerProfileを読み込み、バトルに必要なランタイムオブジェクトを生成するクラス
/// </summary>
public class PlayerDataLoader
{
    private PlayerModelFactory playerFactory;
    private WeaponModelFactory weaponFactory;
    private CardModelFactory cardFactory;
    private int level = 1; // デフォルトレベルを設定

    [Header("ヘルパー関数用変数")]
    private const string ProfileFileName = "player_profile.json";
    private const int MockCharacterCount = 3;
    private const int MockWeaponsPerCharacter = 3;
    private const int MockDirectlyEquippedCardsPerCharacter = 3;
    private const int MockWeaponsWithMinCards = 3;
    private const int MockMinCardsPerWeapon = 3;
    private const int MockMaxCardsPerWeapon = 4;

    public PlayerDataLoader()
    {
        playerFactory = new PlayerModelFactory();
        weaponFactory = new WeaponModelFactory();
        cardFactory = new CardModelFactory();
    }

    /// <summary>
    /// Playerのパーティー情報と全カードリストを読み込んで生成する
    /// </summary>
    public DeckSetupRepository LoadPlayerPartyAndCards()
    {
        PlayerProfile playerProfile = DataManager.LoadData<PlayerProfile>(ProfileFileName);

        // ファイルが存在しない、または中身が空の場合
        if (playerProfile == null || playerProfile.BattleCharacters == null || playerProfile.BattleCharacters.Count == 0)
        {
            playerProfile = CreateAndSaveMockProfile();
        }

        List<PlayerRuntime> party = new List<PlayerRuntime>();

        foreach (var charData in playerProfile.BattleCharacters)
        {
            PlayerModel playerModel = playerFactory.CreateFromId(charData.CharacterId);

            if (playerModel == null)
            {
                Debug.LogError($"PlayerModel生成失敗: ID {charData.CharacterId} が見つかりません。スキップします。");
                continue;
            }

            PlayerRuntime playerRuntime = new PlayerRuntime(playerModel, charData.InstanceId, level);
            party.Add(playerRuntime);

            // プレイヤー直持ちカード
            if (charData.EquippedCards != null)
            {
                foreach (var cardData in charData.EquippedCards)
                {
                    CardModel cardModel = cardFactory.CreateFromID(cardData.CardId);
                    if (cardModel != null)
                    {
                        CardRuntime cardRuntime = new CardRuntime(cardModel, cardData.InstanceId);
                        playerRuntime.CaracterCardWeapon.AddCard(cardRuntime);
                    }
                }
            }

            // 武器とスロットカード
            if (charData.EquippedWeapons != null)
            {
                foreach (var weaponData in charData.EquippedWeapons)
                {
                    WeaponModel weaponModel = weaponFactory.CreateFromId(weaponData.WeaponId);
                    if (weaponModel == null) continue; // nullチェック

                    WeaponRuntime weaponRuntime = new WeaponRuntime(weaponModel, weaponData.InstanceId);
                    playerRuntime.EquipWeapon(weaponRuntime);

                    if (weaponData.SlottedCards != null)
                    {
                        foreach (var cardData in weaponData.SlottedCards)
                        {
                            CardModel cardModel = cardFactory.CreateFromID(cardData.CardId);
                            if (cardModel != null)
                            {
                                CardRuntime cardRuntime = new CardRuntime(cardModel, cardData.InstanceId);
                                weaponRuntime.AddCard(cardRuntime);
                            }
                        }
                    }
                }
            }
        }
        return new DeckSetupRepository(party);
    }

    /// <summary>
    /// 実在するカードIDを使ってモックデータを作成する
    /// </summary>
    private PlayerProfile CreateAndSaveMockProfile()
    {
        Debug.Log("モックのプレイヤープロファイルを作成・保存します...");

        // 実在する全カードデータをロードしてリスト化する
        CardEntity[] allRealCards = Resources.LoadAll<CardEntity>("EntityDataList/CardEntityList");

        if (allRealCards.Length == 0)
        {
            Debug.LogError("モック作成エラー: Resources/EntityDataList/CardEntityList にカードが1枚もありません！");
            return new PlayerProfile();
        }

        // ランダムアクセスしやすいようにリストへ
        List<CardEntity> cardPool = allRealCards.OrderBy(c => c.ID).ToList();
        int cardPoolIndex = 0;

        PlayerProfile profile = new PlayerProfile
        {
            PlayerName = "FirstPlayer",
            BattleCharacters = new List<CharacterData>()
        };

        // 2. キャラクターデータ作成
        for (int i = 1; i <= MockCharacterCount; i++)
        {
            var character = new CharacterData
            {
                InstanceId = System.Guid.NewGuid().ToString(),
                CharacterId = i, // ※PlayerEntityのIDも実在するものに合わせてください
                EquippedCards = new List<CardData>(),
                EquippedWeapons = new List<WeaponData>()
            };

            // 直持ちカード
            for (int c = 0; c < MockDirectlyEquippedCardsPerCharacter; c++)
            {
                // 実在するカードリストからIDを取得（リストを循環させる）
                int realId = cardPool[cardPoolIndex % cardPool.Count].ID;
                cardPoolIndex++;

                character.EquippedCards.Add(new CardData { InstanceId = System.Guid.NewGuid().ToString(), CardId = realId });
            }

            // 武器
            for (int j = 1; j <= MockWeaponsPerCharacter; j++)
            {
                var weapon = new WeaponData
                {
                    InstanceId = System.Guid.NewGuid().ToString(),
                    WeaponId = j,
                    SlottedCards = new List<CardData>()
                };

                // カードスロット
                int numCardsToSlot = (j <= MockWeaponsWithMinCards) ? MockMinCardsPerWeapon : MockMaxCardsPerWeapon;

                for (int k = 0; k < numCardsToSlot; k++)
                {
                    int realId = cardPool[cardPoolIndex % cardPool.Count].ID;
                    cardPoolIndex++;

                    weapon.SlottedCards.Add(new CardData { InstanceId = System.Guid.NewGuid().ToString(), CardId = realId });
                }
                character.EquippedWeapons.Add(weapon);
            }
            profile.BattleCharacters.Add(character);
        }

        // 保存
        DataManager.SaveData(profile, ProfileFileName);
        return profile;
    }
}