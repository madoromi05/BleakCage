using System.Collections.Generic;
using UnityEngine;

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
    private int level;

    [Header("ヘルパー関数用変数")]
    private const string ProfileFileName = "player_profile.json";
    private const int MockCharacterCount = 3;
    private const int MockWeaponsPerCharacter = 3;
    private const int MockDirectlyEquippedCardsPerCharacter = 3;
    private const int MockWeaponsWithMinCards = 3; // 最小枚数のカードを持つ武器の数
    private const int MockMinCardsPerWeapon = 3;   // 武器にスロットする最小カード数
    private const int MockMaxCardsPerWeapon = 4;   // 武器にスロットする最大カード数
    private const int MaxMockCardId = 42;

    public PlayerDataLoader()
    {
        playerFactory = new PlayerModelFactory();
        weaponFactory = new WeaponModelFactory();
        cardFactory = new CardModelFactory();
    }

    /// <summary>
    /// Playerのパーティー情報と全カードリストを読み込んで生成する
    /// </summary>
    /// <returns>生成されたパーティーとカードのデータ</returns>
    public DeckSetupRepository LoadPlayerPartyAndCards()
    {
        PlayerProfile playerProfile = DataManager.LoadData<PlayerProfile>(ProfileFileName);

        // ファイルが存在しない場合、モックデータを作成して次回のために保存する
        if (playerProfile.BattleCharacters == null || playerProfile.BattleCharacters.Count == 0)
        {
            playerProfile = CreateAndSaveMockProfile();
        }

        List<PlayerRuntime> party = new List<PlayerRuntime>();
        IAttackStrategy defaultStrategy = new AttributeWeakness();

        foreach (var charData in playerProfile.BattleCharacters)
        {
            PlayerModel playerModel = playerFactory.CreateFromId(charData.CharacterId);

            if (playerModel == null)
            {
                Debug.LogError($"PlayerModelの生成に失敗しました。ID: {charData.CharacterId} のPlayerEntityがResources内に存在するか確認してください。");
                continue;
            }

            PlayerRuntime playerRuntime = new PlayerRuntime(playerModel, defaultStrategy, charData.InstanceId, level);
            party.Add(playerRuntime);

            // プレイヤーが直接持つカードをCharacterCardWeaponにセット
            if (charData.EquippedCards != null)
            {
                foreach (var cardData in charData.EquippedCards)
                {
                    CardModel cardModel = cardFactory.CreateFromID(cardData.CardId);
                    CardRuntime cardRuntime = new CardRuntime(cardModel, cardData.InstanceId);
                    playerRuntime.CaracterCardWeapon.AddCard(cardRuntime);
                }
            }

            // 装備している武器と、それにスロットされたカードを生成
            if (charData.EquippedWeapons != null)
            {
                foreach (var weaponData in charData.EquippedWeapons)
                {
                    WeaponModel weaponModel = weaponFactory.CreateFromId(weaponData.WeaponId);
                    WeaponRuntime weaponRuntime = new WeaponRuntime(weaponModel, weaponData.InstanceId);
                    playerRuntime.EquipWeapon(weaponRuntime);

                    if (weaponData.SlottedCards != null)
                    {
                        foreach (var cardData in weaponData.SlottedCards)
                        {
                            CardModel cardModel = cardFactory.CreateFromID(cardData.CardId);
                            CardRuntime cardRuntime = new CardRuntime(cardModel, cardData.InstanceId);
                            weaponRuntime.AddCard(cardRuntime);
                        }
                    }
                }
            }
        }
        return new DeckSetupRepository(party);
    }

    /// <summary>
    /// データが存在しない場合にモックデータを作成するヘルパー関数
    /// </summary>
    private PlayerProfile CreateAndSaveMockProfile()
    {
        Debug.Log("モックのプレイヤープロファイルを作成・保存します...");

        PlayerProfile profile = new PlayerProfile
        {
            PlayerName = "FarstPlayer",
            BattleCharacters = new List<CharacterData>()
        };

        int cardIdCounter = 1;
        int totalWeaponCount = 0;

        // ループは MockCharacterCount (1に設定) 回だけ実行される
        for (int i = 1; i <= MockCharacterCount; i++)
        {
            var character = new CharacterData
            {
                InstanceId = System.Guid.NewGuid().ToString(),
                CharacterId = i,
                EquippedCards = new List<CardData>(),
                EquippedWeapons = new List<WeaponData>()
            };

            for (int c = 0; c < MockDirectlyEquippedCardsPerCharacter; c++)
            {
                character.EquippedCards.Add(new CardData { InstanceId = System.Guid.NewGuid().ToString(), CardId = cardIdCounter++ });
            }

            for (int j = 1; j <= MockWeaponsPerCharacter; j++)
            {
                var weapon = new WeaponData
                {
                    InstanceId = System.Guid.NewGuid().ToString(),
                    WeaponId = (i - 1) * MockWeaponsPerCharacter + j,
                    SlottedCards = new List<CardData>()
                };

                int numCardsToSlot = (totalWeaponCount < MockWeaponsWithMinCards) ? MockMinCardsPerWeapon : MockMaxCardsPerWeapon;

                for (int k = 0; k < numCardsToSlot; k++)
                {
                    if (cardIdCounter > MaxMockCardId) break;
                    weapon.SlottedCards.Add(new CardData { InstanceId = System.Guid.NewGuid().ToString(), CardId = cardIdCounter++ });
                }
                character.EquippedWeapons.Add(weapon);
                totalWeaponCount++;
            }
            profile.BattleCharacters.Add(character);
        }

        DataManager.SaveData(profile, ProfileFileName);
        return profile;
    }
}