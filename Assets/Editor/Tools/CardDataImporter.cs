#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class CardDataImporter : AssetPostprocessor
{
    // CSVファイル名（Excelで保存する際の名前）
    private const string CSV_FILE_NAME = "CardEntityData.csv";

    // CSVが置かれているフォルダのパス（部分一致検索用）
    private const string CSV_TARGET_FOLDER = "Script/Data/CardEccelData";

    // 生成されたCardEntityの保存先
    private const string CARD_ENTITY_BASE_PATH = "Assets/Resources/EntityDataList/CardEntityList";

    // 名前検索用のキャッシュ
    private static Dictionary<int, string> _characterNameCache;
    private static Dictionary<int, string> _weaponNameCache;

    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        // 指定のフォルダ内にあるCSVのみを対象にする
        foreach (string path in importedAssets)
        {
            // パスの中に "Script/Data/CardEccelData" が含まれていて、かつファイル名が一致するか
            if (path.Contains(CSV_TARGET_FOLDER) && path.EndsWith(CSV_FILE_NAME))
            {
                // キャッシュをリセット
                _characterNameCache = null;
                _weaponNameCache = null;

                // 読み込み実行
                SyncCardEntitiesFromCSV(path);
                return;
            }
        }
    }

    private static void SyncCardEntitiesFromCSV(string csvPath)
    {
        // 1. プロジェクト内の全CardEntityをロード（ID重複チェック用）
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

        // 2. 名前解決のためのキャッシュを作成
        BuildNameCache();

        // 3. CSV読み込み & 処理
        string[] lines = File.ReadAllLines(csvPath);
        if (lines.Length <= 1) return;

        AssetDatabase.StartAssetEditing();

        try
        {
            // 1行目はヘッダーとしてスキップ
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] values = line.Split(',');

                // 列数チェック（最低限必要な列数）
                if (values.Length < 13)
                {
                    Debug.LogWarning($"[CardImporter] Line {i + 1}: 列数が足りないためスキップ (Data: {line})");
                    continue;
                }

                try
                {
                    int categoryId = int.Parse(values[0]);
                    int ownerId = int.Parse(values[1]);
                    int exclusiveId = int.Parse(values[2]);
                    int calculatedID = (categoryId * 100000) + (ownerId * 100) + exclusiveId;
                    CardEntity targetCard = null;
                    bool isNew = false;

                    // 既存データの更新か、新規作成か
                    if (existingCards.TryGetValue(calculatedID, out targetCard))
                    {
                        existingCards.Remove(calculatedID);
                    }
                    else
                    {
                        targetCard = ScriptableObject.CreateInstance<CardEntity>();
                        isNew = true;
                        targetCard.ID = calculatedID;
                    }

                    // データをセット
                    PopulateCardEntity(targetCard, values);

                    // 保存先パスの決定（フォルダ自動振り分け）
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
                        AssetDatabase.MoveAsset(currentPath, idealPath); // 移動
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

    // --- IDから名前を取得するためのキャッシュ ---
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
            if (entity != null && !_weaponNameCache.ContainsKey((int)entity.ID))
                _weaponNameCache.Add((int)entity.ID, entity.Name);
        }
    }

    // CSVの値をCardEntityにコピー
    private static void PopulateCardEntity(CardEntity card, string[] values)
    {
        // 列数チェック
        if (values.Length < 18)
        {
            Debug.LogWarning($"[CardImporter] 列数が足りません (Data Length: {values.Length})。デフォルト値を設定します。 ID:{card.ID}");
        }

        int categoryId = int.Parse(values[0]); // 1=キャラ, 2=武器
        int ownerId = int.Parse(values[1]);
        int exclusiveId = int.Parse(values[2]);

        card.ID = (categoryId * 100) + (ownerId * 10) + exclusiveId;
        card.OwnerID = ownerId;
        card.ExclusiveID = exclusiveId;
        card.Name = values[3];

        // Enumパース
        card.Type = (CardTypeData)System.Enum.Parse(typeof(CardTypeData), values[4]);
        card.Attribute = (AttributeType)System.Enum.Parse(typeof(AttributeType), values[5]);

        card.AttackCount = int.Parse(values[6]);
        card.TargetCount = int.Parse(values[7]);

        if (System.Enum.TryParse(values[8], out CardTargetScope scope))
        {
            card.TargetScope = scope;
        }
        else
        {
            Debug.LogWarning($"TargetScope Parse Failed: {values[8]}. Defaulting to Single.");
            card.TargetScope = CardTargetScope.Single;
        }

        // 列インデックスが1つずつずれるので注意
        card.Passive = bool.Parse(values[9]);
        card.HitRate = float.Parse(values[10]);
        card.OutputModifier = float.Parse(values[11]);
        card.DefensePenetration = float.Parse(values[12]);
        card.IsMelee = bool.Parse(values[13]);

        // 異常状態データの読み込み (列位置が変わっています)
        // StatusTypeは 14列目
        if (values.Length >= 18)
        {
            StatusEffectData statusData = new StatusEffectData();

            if (System.Enum.TryParse(values[14], out StatusEffectType statType))
            {
                statusData.Type = statType;
            }
            else
            {
                statusData.Type = StatusEffectType.None;
            }

            statusData.Value = float.Parse(values[15]);         // 効果値
            statusData.Duration = int.Parse(values[16]);        // 持続ターン
            statusData.InflictStacks = int.Parse(values[17]);   // 付与スタック

            card.StatusEffect = statusData;
        }

        card.name = $"Card_{card.ID}";
    }

    // --- 保存先のパス生成 ---
    private static string GetReadableSavePath(CardEntity card)
    {
        string subFolder = "Uncategorized";
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
            subFolder = Path.Combine("Weapon", $"Weapon_{wpId}_{wpName}");
        }
        else
        {
            subFolder = "Common";
        }

        // ここで最終的な保存先パスを作成
        string fileName = $"Card_{card.ID}.asset";
        return Path.Combine(CARD_ENTITY_BASE_PATH, subFolder, fileName).Replace("\\", "/");
    }
}
#endif