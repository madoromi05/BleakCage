//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using UnityEditor;
//using UnityEditor.AddressableAssets; // Addressablesを操作するために必要
//using UnityEditor.AddressableAssets.Settings;
//using UnityEngine;
//using UnityEngine.AddressableAssets; // Addressablesを操作するために必要

///// <summary>
///// CSVファイルから CardEntity (ScriptableObject) アセットを
///// 一括で「作成」「更新」「削除」するインポーター
///// </summary>
//public class CardDataImporter : AssetPostprocessor
//{
//    // 同期するCSVファイル名 (プロジェクト内のどこにあってもOK)
//    private const string CSV_FILE_NAME = "CardEntityData.csv";

//    // CardEntityアセットを保存するベースフォルダ
//    private const string CARD_ENTITY_BASE_PATH = "Assets/Resources/CardEntityList";

//    // Addressablesのラベル
//    private const string ADDRESSABLES_LABEL = "CardEntity";

//    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
//    {
//        // インポートされたファイルにCSV_FILE_NAMEが含まれているかチェック
//        bool csvImported = importedAssets.Any(path => path.EndsWith(CSV_FILE_NAME));

//        // CSVが削除された場合も処理（全削除など）を行う場合は、deletedAssetsもチェック
//        // bool csvDeleted = deletedAssets.Any(path => path.EndsWith(CSV_FILE_NAME));

//        if (csvImported)
//        {
//            Debug.Log($"[{CSV_FILE_NAME}] の変更を検出。CardEntityアセットの同期を開始します。");

//            // CSVファイルの完全パスを取得
//            string csvPath = importedAssets.First(path => path.EndsWith(CSV_FILE_NAME));

//            // 同期処理を実行
//            SyncCardEntitiesFromCSV(csvPath);
//        }
//    }

//    private static void SyncCardEntitiesFromCSV(string csvPath)
//    {
//        // 1. 既存の全CardEntityアセットをロードし、IDをキーにした辞書を作成
//        var existingCards = new Dictionary<int, CardEntity>();
//        var settings = AddressableAssetSettingsDefaultObject.Settings;

//        // "t:CardEntity" でプロジェクト内の全CardEntityアセットを検索
//        string[] guids = AssetDatabase.FindAssets("t:CardEntity");
//        foreach (string guid in guids)
//        {
//            string path = AssetDatabase.GUIDToAssetPath(guid);
//            CardEntity entity = AssetDatabase.LoadAssetAtPath<CardEntity>(path);
//            if (entity != null && !existingCards.ContainsKey(entity.ID))
//            {
//                existingCards.Add(entity.ID, entity);
//            }
//        }

//        // 2. CSVファイルを読み込む
//        string[] lines = File.ReadAllLines(csvPath);
//        if (lines.Length <= 1)
//        {
//            Debug.LogWarning("CSVファイルにヘッダー行以外データがありません。");
//            return;
//        }

//        // アセットの変更処理を開始することをUnityに通知
//        AssetDatabase.StartAssetEditing();

//        var settingsUpdated = false;

//        try
//        {
//            // 3. 1行ずつパース (i=0 はヘッダーなのでスキップ)
//            for (int i = 1; i < lines.Length; i++)
//            {
//                string line = lines[i];
//                if (string.IsNullOrWhiteSpace(line)) continue;

//                // 説明文にカンマが含まれているとパースが崩れます。
//                // 本格的に運用する場合、CsvHelperなどの専用ライブラリの導入すること。
//                string[] values = line.Split(',');

//                if (values.Length < 14) // 列が足りない行はスキップ
//                {
//                    Debug.LogWarning($"CSV {i + 1}行目: 列数が不足しています。スキップします。 (Line: {line})");
//                    continue;
//                }

//                try
//                {
//                    int id = int.Parse(values[0]);

//                    // 4. アセットの「更新」または「新規作成」
//                    if (existingCards.TryGetValue(id, out CardEntity targetCard))
//                    {
//                        // 4a. [更新] 既存アセットのデータを上書き
//                        PopulateCardEntity(targetCard, values);
//                        EditorUtility.SetDirty(targetCard);

//                        // 既存リストから削除（＝処理済み）
//                        existingCards.Remove(id);
//                    }
//                    else
//                    {
//                        // 4b. [新規作成] 新しいアセットを作成
//                        targetCard = ScriptableObject.CreateInstance<CardEntity>();
//                        PopulateCardEntity(targetCard, values);

//                        // 保存パスを決定
//                        string path = GetSavePath(targetCard);
//                        Directory.CreateDirectory(Path.GetDirectoryName(path));
//                        string uniquePath = AssetDatabase.GenerateUniqueAssetPath(path);

//                        AssetDatabase.CreateAsset(targetCard, uniquePath);

//                        // Addressablesに登録
//                        if (settings != null)
//                        {
//                            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(uniquePath), settings.DefaultGroup);
//                            entry.SetLabel(ADDRESSABLES_LABEL, true);
//                            settingsUpdated = true;
//                        }
//                    }
//                }
//                catch (System.Exception ex)
//                {
//                    Debug.LogError($"CSV {i + 1}行目の処理に失敗しました: {ex.Message} (Line: {line})");
//                }
//            }

//            // 5. [削除] CSVに存在しなかった既存アセットを削除
//            if (existingCards.Any())
//            {
//                Debug.Log($"{existingCards.Count}件の古いCardEntityアセットを削除します...");
//                foreach (var pair in existingCards)
//                {
//                    CardEntity cardToDelete = pair.Value;
//                    string path = AssetDatabase.GetAssetPath(cardToDelete);

//                    // Addressablesから削除
//                    if (settings != null)
//                    {
//                        var guid = AssetDatabase.AssetPathToGUID(path);
//                        settings.RemoveAssetEntry(guid);
//                        settingsUpdated = true;
//                    }

//                    // アセットファイルを削除
//                    AssetDatabase.DeleteAsset(path);
//                    Debug.Log($"Deleted: {path}");
//                }
//            }

//            if (settingsUpdated)
//            {
//                // Addressablesの設定変更を保存
//                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
//            }
//        }
//        finally
//        {
//            // アセットの変更処理を完了
//            AssetDatabase.StopAssetEditing();
//            AssetDatabase.SaveAssets();
//            AssetDatabase.Refresh();
//        }

//        Debug.Log($"[{CSV_FILE_NAME}] からの CardEntity 同期が完了しました。");
//    }

//    /// <summary>
//    /// CSVの1行データ(values)を CardEntity インスタンスに書き込みます。
//    /// </summary>
//    private static void PopulateCardEntity(CardEntity card, string[] values)
//    {
//        // CSVの列の順番に合わせてパースします
//        // 0: ID
//        card.ID = int.Parse(values[0]);
//        // 1: Name
//        card.Name = values[1];
//        // 2: CharacterID
//        card.CharacterID = int.Parse(values[2]);
//        // 3: EquipableWeaponID
//        card.EquipableWeaponID = int.Parse(values[3]);
//        // 4: Icon (アセットパスを想定)
//        card.Icon = AssetDatabase.LoadAssetAtPath<Sprite>(values[4]);
//        // 5: Description
//        card.Description = values[5].Replace("\\n", "\n"); // CSV内で \n を改行として扱う
//        // 6: Type
//        card.Type = (CardTypeData)System.Enum.Parse(typeof(CardTypeData), values[6]);
//        // 7: Attribute
//        card.Attribute = (AttributeType)System.Enum.Parse(typeof(AttributeType), values[7]);
//        // 8: AttackCount
//        card.AttackCount = int.Parse(values[8]);
//        // 9: TargetCount
//        card.TargetCount = int.Parse(values[9]);
//        // 10: Passive (TRUE/FALSE)
//        card.Passive = bool.Parse(values[10].ToUpper());
//        // 11: HitRate
//        card.HitRate = float.Parse(values[11]);
//        // 12: OutputModifier
//        card.OutputModifier = float.Parse(values[12]);
//        // 13: DefensePenetration
//        card.DefensePenetration = float.Parse(values[13]);

//        // [注意] アセット名もIDと同期させます
//        if (card.name != $"Card_{card.ID}")
//        {
//            card.name = $"Card_{card.ID}";
//        }
//    }

//    /// <summary>
//    /// 元の CardEntityTableEditor のロジックに基づき、アセットの保存パスを決定します。
//    /// </summary>
//    private static string GetSavePath(CardEntity card)
//    {
//        string subFolder = card.Type.ToString();

//        if (card.Type == CardTypeData.Character)
//        {
//            subFolder = Path.Combine(subFolder, $"CharID_{card.CharacterID}");
//        }
//        else if (card.Type == CardTypeData.Weapon)
//        {
//            subFolder = Path.Combine(subFolder, $"WeaponID_{card.EquipableWeaponID}");
//        }

//        string fileName = $"Card_{card.ID}.asset";
//        return Path.Combine(CARD_ENTITY_BASE_PATH, subFolder, fileName);
//    }
//}