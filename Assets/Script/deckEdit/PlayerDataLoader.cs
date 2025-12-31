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

    public PlayerDataLoader()
    {
        playerFactory = new PlayerModelFactory();
        weaponFactory = new WeaponModelFactory();
    }

    /// <summary>
    /// ScriptableObject (プリセットデータ) を読み込んで、ランタイム用のデータに変換する
    /// </summary>
    public DeckSetupRepository LoadFromPreset(StagePlayerSetup preset)
    {
        List<PlayerRuntime> party = new List<PlayerRuntime>();

        if (preset == null)
        {
            DebugCostom.LogError("プリセットデータがnullです！");
            return new DeckSetupRepository(party);
        }

        // 設定されている全キャラクターをループ
        foreach (var charData in preset.PartyMembers)
        {
            // --- 1. キャラクター生成 ---
            PlayerModel model = playerFactory.CreateFromId(charData.CharacterID);
            if (model == null)
            {
                DebugCostom.LogError($"CharacterID: {charData.CharacterID} が見つかりません。スキップします。");
                continue;
            }

            // PlayerRuntime生成
            PlayerRuntime playerRuntime = new PlayerRuntime(model, System.Guid.NewGuid().ToString(), charData.Level);

            // ---  プリセット武器 (Deck) のロード ---
            if (charData.Weapons != null && charData.Weapons.Count > 0)
            {
                foreach (var weaponData in charData.Weapons)
                {
                    // IDから武器生成
                    WeaponModel wModel = weaponFactory.CreateFromId(weaponData.WeaponID);

                    if (wModel != null)
                    {
                        WeaponRuntime wRuntime = new WeaponRuntime(wModel, System.Guid.NewGuid().ToString());

                        // WeaponSetupDataにはカードリストがないため、WeaponModelのDefaultCardsを採用する
                        if (wModel.DefaultCards != null)
                        {
                            foreach (CardEntity cardEntity in wModel.DefaultCards)
                            {
                                if (cardEntity == null) continue;
                                CardModel cModel = new CardModel(cardEntity);
                                CardRuntime cRuntime = new CardRuntime(cModel, System.Guid.NewGuid().ToString());
                                wRuntime.AddCard(cRuntime);
                            }
                        }

                        // プレイヤーに装備
                        playerRuntime.EquipWeapon(wRuntime);
                    }
                    else
                    {
                        DebugCostom.LogWarning($"WeaponID: {weaponData.WeaponID} の生成に失敗しました。");
                    }
                }
            }
            party.Add(playerRuntime);
        }

        return new DeckSetupRepository(party);
    }
}