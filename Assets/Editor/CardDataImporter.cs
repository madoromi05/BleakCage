#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class CardDataImporter : AssetPostprocessor
{
    private const string CSV_FILE_NAME = "CardEntityData.csv";
    private const string CSV_TARGET_FOLDER = "Resources/CardEccelData";
    private const string CARD_ENTITY_BASE_PATH = "Assets/Resources/EntityDataList/CardEntityList";

    // 名前検索用のキャッシュ（フォルダ名生成用）
    private static Dictionary<int, string> _characterNameCache;
    private static Dictionary<int, string> _weaponNameCache;

    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string path in importedAssets)
        {
            if (path.Contains(CSV_TARGET_FOLDER) && path.EndsWith(CSV_FILE_NAME))
            {
                // キャッシュをリセット
                _characterNameCache = null;
                _weaponNameCache = null;

                SyncCardEntitiesFromCSV(path);
                return;
            }
        }
    }

    private static void SyncCardEntitiesFromCSV(string csvPath)
    {
        BuildNameCache();
        var existingCards = new Dictionary<int, CardEntity>();
        string[] guids = AssetDatabase.FindAssets("t:CardEntity");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CardEntity entity = AssetDatabase.LoadAssetAtPath<CardEntity>(path);
            if (entity != null && !existingCards.ContainsKey(entity.ID))
            {
                existingCards.Add(entity.ID, entity);
            }
        }

        // CSV読み込み
        string[] lines = File.ReadAllLines(csvPath);
        if (lines.Length <= 1) return;

        AssetDatabase.StartAssetEditing();

        try
        {
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] values = line.Split(',');

                if (values.Length < 16)
                {
                    Debug.LogWarning($"[CardImporter] Line {i + 1}: 列数が足りません。スキップします。");
                    continue;
                }

                try
                {
                    if (!int.TryParse(values[0], out int cardID))
                    {
                        Debug.LogError($"[CardImporter] Line {i + 1}: IDが数値ではありません ({values[0]})");
                        continue;
                    }

                    CardEntity targetCard = null;
                    bool isNew = false;

                    if (existingCards.TryGetValue(cardID, out targetCard))
                    {
                        existingCards.Remove(cardID);
                    }
                    else
                    {
                        targetCard = ScriptableObject.CreateInstance<CardEntity>();
                        isNew = true;
                        targetCard.ID = cardID;
                    }

                    // データをセット
                    PopulateCardEntity(targetCard, values);

                    string idealPath = GetReadableSavePath(targetCard);
                    string currentPath = isNew ? "" : AssetDatabase.GetAssetPath(targetCard);
                    string directory = Path.GetDirectoryName(idealPath);

                    if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                    if (isNew)
                    {
                        AssetDatabase.CreateAsset(targetCard, idealPath);
                    }
                    else if (currentPath != idealPath)
                    {
                        AssetDatabase.MoveAsset(currentPath, idealPath);
                        EditorUtility.SetDirty(targetCard);
                    }
                    else
                    {
                        EditorUtility.SetDirty(targetCard);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[CardImporter] Error Line {i + 1}: {ex.Message}");
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"CardEntity Import Complete: {csvPath}");
    }

    private static void BuildNameCache()
    {
        _characterNameCache = new Dictionary<int, string>();
        _weaponNameCache = new Dictionary<int, string>();

        foreach (var guid in AssetDatabase.FindAssets("t:PlayerEntity"))
        {
            var entity = AssetDatabase.LoadAssetAtPath<PlayerEntity>(AssetDatabase.GUIDToAssetPath(guid));
            if (entity != null && !_characterNameCache.ContainsKey(entity.PlayerId))
                _characterNameCache.Add(entity.PlayerId, entity.PlayerName);
        }

        foreach (var guid in AssetDatabase.FindAssets("t:WeaponEntity"))
        {
            var entity = AssetDatabase.LoadAssetAtPath<WeaponEntity>(AssetDatabase.GUIDToAssetPath(guid));
            if (entity != null && !_weaponNameCache.ContainsKey(entity.ID))
                _weaponNameCache.Add(entity.ID, entity.Name);
        }
    }

    private static void PopulateCardEntity(CardEntity card, string[] values)
    {
        int ownerID = int.Parse(values[1]);
        card.OwnerID = ownerID;
        card.Name = values[2];
        card.Type = ParseEnum<CardTypeData>(values[3]);
        card.Attribute = ParseEnum<AttributeType>(values[4]);

        if (card.Type == CardTypeData.Character)
        {
            card.CharacterID = ownerID;
            card.EquipableWeaponID = 0;
        }
        else if (card.Type == CardTypeData.Weapon)
        {
            card.CharacterID = 0;
            card.EquipableWeaponID = ownerID;
        }

        card.AttackCount = int.Parse(values[5]);
        card.TargetCount = int.Parse(values[6]);

        if (!System.Enum.TryParse(values[7], out CardTargetScope scope))
            scope = CardTargetScope.Single;
        card.TargetScope = scope;

        card.Passive = ParseBool(values[8]);
        card.HitRate = float.Parse(values[9]);
        card.OutputModifier = float.Parse(values[10]);
        card.DefensePenetration = float.Parse(values[11]);
        card.IsMelee = ParseBool(values[12]);

        // 状態異常
        StatusEffectData statusData = new StatusEffectData();
        statusData.Type = ParseEnum<StatusEffectType>(values[13]);
        statusData.Value = float.Parse(values[14]);
        statusData.Duration = int.Parse(values[15]);
        statusData.InflictStacks = (values.Length > 16) ? int.Parse(values[16]) : 0;

        card.StatusEffect = statusData;
        card.name = $"Card_{card.ID}_{card.Name}";
    }

    private static string GetReadableSavePath(CardEntity card)
    {
        string subFolder = "Uncategorized";
        // ファイル名に使えない文字を除去するローカル関数
        string Sanitize(string name) => string.Join("_", name.Split(Path.GetInvalidFileNameChars()));

        if (card.Type == CardTypeData.Character)
        {
            int charId = card.CharacterID;
            string charName = (_characterNameCache != null && _characterNameCache.TryGetValue(charId, out string n)) ? Sanitize(n) : "Unknown";
            subFolder = Path.Combine("Character", $"Char_{charId:D2}_{charName}");
        }
        else if (card.Type == CardTypeData.Weapon)
        {
            int wpId = card.EquipableWeaponID;
            string wpName = (_weaponNameCache != null && _weaponNameCache.TryGetValue(wpId, out string n)) ? Sanitize(n) : "Unknown";

            subFolder = Path.Combine("Weapon", $"Weapon_{wpId:D2}_{wpName}");
        }
        else
        {
            subFolder = "Common";
        }

        string fileName = $"Card_{card.ID}_{card.Name}.asset";
        return Path.Combine(CARD_ENTITY_BASE_PATH, subFolder, fileName).Replace("\\", "/");
    }

    private static T ParseEnum<T>(string value) where T : struct
    {
        if (System.Enum.TryParse(value, out T result)) return result;
        return default(T);
    }

    private static bool ParseBool(string value)
    {
        if (bool.TryParse(value, out bool result)) return result;
        if (value == "1") return true;
        return false;
    }
}
#endif