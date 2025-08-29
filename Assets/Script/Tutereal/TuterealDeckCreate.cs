using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 初期デッキ（モックのプレイヤープロファイル）を作成するためのクラス
/// </summary>
public static class TuterealDeckCreate
{
    /// <summary>
    /// データが存在しない場合にモックデータを作成し、保存する。
    /// これが初期デッキ（キャラクター、武器、カードの構成）になる。
    /// </summary>
    /// <returns>作成されたプレイヤープロファイル</returns>
    public static PlayerProfile CreateAndSaveMockProfile()
    {
        Debug.Log("モックのプレイヤープロファイル（キャラクター1体）を作成・保存します...");

        PlayerProfile profile = new PlayerProfile
        {
            PlayerName = "FarstPlayer",
            BattleCharacters = new List<CharacterData>()
        };

        int cardIdCounter = 1;      // カードIDを1から順に割り振る

        // 1体のプレイヤーキャラクターを作成
        var character = new CharacterData
        {
            InstanceId = System.Guid.NewGuid().ToString(),
            CharacterId = 1, // キャラクターIDを 1 に固定
            EquippedCards = new List<CardData>(),
            EquippedWeapons = new List<WeaponData>()
        };

        // キャラクターに直接3枚のカードを持たせる
        for (int c = 0; c < 3; c++)
        {
            character.EquippedCards.Add(new CardData { InstanceId = System.Guid.NewGuid().ToString(), CardId = cardIdCounter });
            cardIdCounter++;
        }

        // キャラクターに3つの武器を装備させる
        for (int j = 1; j <= 3; j++)
        {
            var weapon = new WeaponData
            {
                InstanceId = System.Guid.NewGuid().ToString(),
                WeaponId = j, // 武器IDを 1～3 で割り振る
                SlottedCards = new List<CardData>()
            };

            // 各武器に3枚のカードをセット
            int numCardsToSlot = 3;

            for (int k = 0; k < numCardsToSlot; k++)
            {
                weapon.SlottedCards.Add(new CardData
                {
                    InstanceId = System.Guid.NewGuid().ToString(),
                    CardId = cardIdCounter
                });
                cardIdCounter++;
            }

            character.EquippedWeapons.Add(weapon);
        }

        // 作成したキャラクターをプロファイルに追加
        profile.BattleCharacters.Add(character);

        DataManager.SaveData(profile, "player_profile.json");
        return profile;
    }
}