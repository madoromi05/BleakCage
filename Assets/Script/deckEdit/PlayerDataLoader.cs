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
            if (model.PlayerWeapon == null)
            {
                Debug.LogError($"[異常] PlayerModelに武器が入っていません！ ID: {model.PlayerID}, Name: {model.PlayerName}");
            }
            // Playerについている武器
            foreach (var weaponData in charData.Weapons)
            {
                WeaponModel wModel = weaponFactory.CreateFromId(weaponData.WeaponID);
                if (wModel != null)
                {
                    WeaponRuntime wRuntime = new WeaponRuntime(wModel, System.Guid.NewGuid().ToString());
                    if (wModel.DefaultCards != null)
                    {
                        foreach (CardEntity cardEntity in wModel.DefaultCards)
                        {
                            CardModel cModel = new CardModel(cardEntity);
                            CardRuntime cRuntime = new CardRuntime(cModel, System.Guid.NewGuid().ToString());
                            wRuntime.AddCard(cRuntime);
                        }
                    }
                    playerRuntime.EquipWeapon(wRuntime);
                }
            }
            if (model.PlayerWeapon != null)
            {
                WeaponModel defaultWModel = new WeaponModel(model.PlayerWeapon);
                WeaponRuntime defaultWRuntime = new WeaponRuntime(defaultWModel, System.Guid.NewGuid().ToString());

                if (defaultWModel.DefaultCards != null)
                {
                    foreach (CardEntity cardEntity in defaultWModel.DefaultCards)
                    {
                        CardModel cModel = new CardModel(cardEntity);
                        CardRuntime cRuntime = new CardRuntime(cModel, System.Guid.NewGuid().ToString());
                        defaultWRuntime.AddCard(cRuntime);
                    }
                }
                playerRuntime.EquipWeapon(defaultWRuntime);
            }

            party.Add(playerRuntime);
        }

        return new DeckSetupRepository(party);
    }
}