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
            Debug.LogError("プリセットデータがnullです！");
            return new DeckSetupRepository(party);
        }

        // 設定されている全キャラクターをループ
        foreach (var charData in preset.PartyMembers)
        {
            // --- 1. キャラクター生成 ---
            PlayerModel model = playerFactory.CreateFromId(charData.CharacterID);
            if (model == null)
            {
                Debug.LogError($"CharacterID: {charData.CharacterID} が見つかりません。スキップします。");
                continue;
            }

            // PlayerRuntime生成
            PlayerRuntime playerRuntime = new PlayerRuntime(model, System.Guid.NewGuid().ToString(), charData.Level);

            // 武器を装備したかどうかのフラグ
            bool hasEquippedAnyWeapon = false;

            // --- 2. プリセット武器 (Deck) のロード ---
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
                                // CardEntity -> CardModel -> CardRuntime
                                CardModel cModel = new CardModel(cardEntity);
                                CardRuntime cRuntime = new CardRuntime(cModel, System.Guid.NewGuid().ToString());
                                wRuntime.AddCard(cRuntime);
                            }
                        }

                        // プレイヤーに装備
                        playerRuntime.EquipWeapon(wRuntime);
                        hasEquippedAnyWeapon = true;
                    }
                    else
                    {
                        Debug.LogWarning($"WeaponID: {weaponData.WeaponID} の生成に失敗しました。");
                    }
                }
            }

            // --- 3. デフォルト武器のロード (フォールバック) ---
            // ★修正ポイント: プリセットで武器が1つも指定されていない場合のみ、キャラ固有の初期武器を持たせる
            if (!hasEquippedAnyWeapon && model.PlayerWeapon != null)
            {
                // 初期武器(Exclusive Weapon)のデータをモデル化
                WeaponModel defaultWModel = new WeaponModel(model.PlayerWeapon);
                WeaponRuntime defaultWRuntime = new WeaponRuntime(defaultWModel, System.Guid.NewGuid().ToString());

                // 初期武器のカード追加
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
            else if (!hasEquippedAnyWeapon && model.PlayerWeapon == null)
            {
                // プリセットなし、かつ初期武器も設定されていない場合
                Debug.LogWarning($"[Loader] {model.PlayerName} (ID:{model.PlayerID}) は装備武器を持っていません。");
            }

            party.Add(playerRuntime);
        }

        return new DeckSetupRepository(party);
    }
}