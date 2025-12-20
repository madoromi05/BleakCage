using System.Collections.Generic;
using UnityEngine;

public class DeckSetupRepository
{
    public List<PlayerRuntime> Party { get; }

    public DeckSetupRepository(List<PlayerRuntime> party)
    {
        Party = party;
    }
}
public class PlayerDataLoader
{
    private PlayerModelFactory playerFactory;
    private WeaponModelFactory weaponFactory;
    private CardModelFactory cardFactory;

    public PlayerDataLoader()
    {
        playerFactory = new PlayerModelFactory();
        weaponFactory = new WeaponModelFactory();
        cardFactory = new CardModelFactory();
    }

    /// <summary>
    /// ScriptableObject (プリセットデータ) を読み込んで、ランタイム用のデータに変換する
    /// </summary>
    public DeckSetupRepository LoadFromPreset(StagePlayerSetup preset)
    {
        List<PlayerRuntime> party = new List<PlayerRuntime>();

        if (preset == null)
        {
            Debug.LogError("プリセットデータがnullです！");
            return new DeckSetupRepository(party);
        }

        // 設定されている全キャラクターをループ
        foreach (var charData in preset.PartyMembers)
        {
            // キャラ生成
            PlayerModel model = playerFactory.CreateFromId(charData.CharacterID);
            if (model == null)
            {
                Debug.LogError($"CharacterID: {charData.CharacterID} が見つかりません。スキップします。");
                continue;
            }

            PlayerRuntime playerRuntime = new PlayerRuntime(model, System.Guid.NewGuid().ToString(), charData.Level);

            // 直持ちカードの生成 & 追加
            foreach (int cardId in charData.DirectCardIDs)
            {
                CardModel cModel = cardFactory.CreateFromID(cardId);
                if (cModel != null)
                {
                    CardRuntime cRuntime = new CardRuntime(cModel, System.Guid.NewGuid().ToString());
                    playerRuntime.CaracterCardWeapon.AddCard(cRuntime);
                }
                else
                {
                    Debug.LogWarning($"PlayerDataLoader: CardID {cardId} が見つかりません。");
                }
            }

            // 武器の生成 & 装備
            foreach (var weaponData in charData.Weapons)
            {
                WeaponModel wModel = weaponFactory.CreateFromId(weaponData.WeaponID);
                if (wModel != null)
                {
                    WeaponRuntime wRuntime = new WeaponRuntime(wModel, System.Guid.NewGuid().ToString());

                    // 武器スロットのカード生成
                    foreach (int cardId in weaponData.SlottedCardIDs)
                    {
                        CardModel cModel = cardFactory.CreateFromID(cardId);
                        if (cModel != null)
                        {
                            CardRuntime cRuntime = new CardRuntime(cModel, System.Guid.NewGuid().ToString());
                            wRuntime.AddCard(cRuntime);
                        }
                        else
                        {
                            Debug.LogWarning($"PlayerDataLoader: CardID {cardId} が見つかりません。");
                        }
                    }
                    playerRuntime.EquipWeapon(wRuntime);
                }
            }
            party.Add(playerRuntime);
        }

        return new DeckSetupRepository(party);
    }
}