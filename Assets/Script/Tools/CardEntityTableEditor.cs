using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Linq;
using System.IO;
using UnityEditor.AddressableAssets; // AddressableAssetSettingsDefaultObject.Settings を使うために必要

/// <summary>
/// CardのGUIでのデータ管理ツール（修正版）
/// </summary>
public class CardEntityTableEditor : EditorWindow
{
    private Vector2 scrollPos;
    private List<CardEntity> cardList;
    private bool isLoading = false; // << MOD: 非同期ロード中の状態を管理するフラグ
    private HashSet<int> duplicateIds = new HashSet<int>(); // << ADD: 重複IDを保持するセット

    [MenuItem("Tools/Card Entity Table")]
    public static void OpenWindow()
    {
        GetWindow<CardEntityTableEditor>("Card Table");
    }

    private void OnEnable()
    {
        LoadData();
    }

    /// <summary>
    /// AddressablesからCardEntityを非同期でロードします。
    /// </summary>
    private async void LoadData()
    {
        // << MOD: ロード中に再度呼び出されるのを防ぐ
        if (isLoading) return;
        isLoading = true;

        // << MOD: cardListをクリアしてロード中であることを示す
        cardList = null;
        Repaint();

        try
        {
            AsyncOperationHandle<IList<CardEntity>> handle =
                Addressables.LoadAssetsAsync<CardEntity>("CardEntity", null);

            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                // << MOD: ロード成功時に一度だけソートする
                cardList = handle.Result?.Where(c => c != null).OrderBy(c => c.ID).ToList() ?? new List<CardEntity>();
                CheckForDuplicateIDs(); // << ADD: 重複IDのチェック
            }
            else
            {
                Debug.LogWarning($"CardEntity のロードに失敗しました（Status: {handle.Status}）");
                cardList = new List<CardEntity>(); // << MOD: 失敗時もリストを初期化してエラーを防ぐ
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"An error occurred during data loading: {ex.Message}");
            cardList = new List<CardEntity>();
        }
        finally
        {
            isLoading = false;
            Repaint(); // 完了後にUIを再描画
        }
    }

    private void OnGUI()
    {
        // << MOD: ロード中のUI表示
        if (isLoading)
        {
            EditorGUILayout.LabelField("データをロード中...", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        if (cardList == null)
        {
            // OnEnable直後など、まだリストが準備できていない場合に表示
            EditorGUILayout.LabelField("初期化中...", EditorStyles.centeredGreyMiniLabel);
            // OnEnableでLoadDataが呼ばれているので、ここでは何もしない
            return;
        }

        DrawToolbar(); // << REFACTOR: ツールバー部分をメソッドに分割
        EditorGUILayout.Space();
        DrawCardTable(); // << REFACTOR: カード一覧テーブル部分をメソッドに分割
        DrawFooter(); // << REFACTOR: フッター部分をメソッドに分割
    }

    /// <summary>
    /// ツールバー（リロード、新規作成ボタン）を描画します。
    /// </summary>
    private void DrawToolbar()
    {
        EditorGUILayout.BeginVertical("box");
        if (GUILayout.Button("Reload Cards"))
        {
            LoadData();
        }
        EditorGUILayout.Space();
        if (GUILayout.Button("新規作成", GUILayout.Width(100)))
        {
            CreateNewCard();
        }
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// カードデータの一覧テーブルを描画します。
    /// </summary>
    private void DrawCardTable()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        DrawHeader(); // ヘッダーを描画

        // << MOD: OnGUI内で毎回ソートするのをやめ、パフォーマンスを向上させる
        // cardList = cardList.OrderBy(c => c.ID).ToList();

        List<CardEntity> toDelete = new List<CardEntity>();

        foreach (var card in cardList)
        {
            if (card == null) continue;
            DrawCardRow(card, toDelete); // 各カードの行を描画
        }

        // 削除処理
        if (toDelete.Any())
        {
            foreach (var card in toDelete)
            {
                DeleteCard(card);
            }
            // << MOD: 全データをリロードするのではなく、リストから直接削除してUIの応答性を上げる
            cardList.RemoveAll(toDelete.Contains);
            CheckForDuplicateIDs();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// テーブルのヘッダーを描画します。
    /// </summary>
    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("ID", GUILayout.Width(40));
        GUILayout.Label("名前", GUILayout.Width(120));
        GUILayout.Label("Type", GUILayout.Width(80));
        GUILayout.Label("装備可能武器ID", GUILayout.Width(150));
        GUILayout.Label("攻撃属性", GUILayout.Width(80));
        GUILayout.Label("出力調整", GUILayout.Width(80));
        GUILayout.Label("命中率", GUILayout.Width(80));
        GUILayout.Label("防御貫通率", GUILayout.Width(80));
        GUILayout.Label("攻撃回数", GUILayout.Width(80));
        GUILayout.Label("攻撃対数", GUILayout.Width(80));
        GUILayout.Label("Passive", GUILayout.Width(60));
        GUILayout.Label("Icon", GUILayout.Width(60));
        GUILayout.Label("説明文", GUILayout.ExpandWidth(true));
        GUILayout.Label("Actions", GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// カード1枚分の行を描画します。
    /// </summary>
    private void DrawCardRow(CardEntity card, List<CardEntity> toDelete)
    {
        EditorGUILayout.BeginHorizontal();

        // << ADD: IDが重複している場合は背景色を変えて警告する
        Color originalColor = GUI.backgroundColor;
        if (duplicateIds.Contains(card.ID))
        {
            GUI.backgroundColor = Color.yellow;
        }

        EditorGUI.BeginChangeCheck();

        // --- 各プロパティのフィールド ---
        int newId = EditorGUILayout.IntField(card.ID, GUILayout.Width(40));
        if (newId != card.ID)
        {
            card.ID = newId;
            // << ADD: IDが変更されたらリストをソートし、重複チェックを行う
            cardList = cardList.OrderBy(c => c.ID).ToList();
            CheckForDuplicateIDs();
        }
        GUI.backgroundColor = originalColor; // IDフィールドの色を元に戻す

        card.Name = EditorGUILayout.TextField(card.Name, GUILayout.Width(120));
        card.Type = (CardEntity.CardTypeData)EditorGUILayout.EnumPopup(card.Type, GUILayout.Width(80));

        // 装備可能武器ID入力欄
        string weaponIds = string.Join(",", card.EquipableWeaponID ?? new int[0]);
        string newWeaponIds = EditorGUILayout.TextField(weaponIds, GUILayout.Width(150));
        if (newWeaponIds != weaponIds)
        {
            // カンマ区切りの文字列をint配列に変換
            try
            {
                card.EquipableWeaponID = string.IsNullOrEmpty(newWeaponIds)
                    ? new int[0]
                    : newWeaponIds.Split(',').Select(s => int.Parse(s.Trim())).ToArray();
            }
            catch
            {
                Debug.LogError("装備可能武器IDの入力に誤りがあります。整数をカンマ区切りで入力してください。例: 1,5,10");
            }
        }

        card.Attribute = (AttributeType)EditorGUILayout.EnumPopup(card.Attribute, GUILayout.Width(80));
        card.OutputModifier = EditorGUILayout.FloatField(card.OutputModifier, GUILayout.Width(80));
        card.HitRate = EditorGUILayout.FloatField(card.HitRate, GUILayout.Width(80));
        card.DefensePenetration = EditorGUILayout.FloatField(card.DefensePenetration, GUILayout.Width(80));
        card.AttackCount = EditorGUILayout.IntField(card.AttackCount, GUILayout.Width(80));
        card.TargetCount = EditorGUILayout.IntField(card.TargetCount, GUILayout.Width(80));
        card.Passive = EditorGUILayout.Toggle(card.Passive, GUILayout.Width(60));
        card.Icon = (Sprite)EditorGUILayout.ObjectField(card.Icon, typeof(Sprite), false, GUILayout.Width(60));
        card.Description = EditorGUILayout.TextArea(card.Description, GUILayout.ExpandWidth(true), GUILayout.Height(40));

        // --- 変更の保存 ---
        if (EditorGUI.EndChangeCheck())
        {
            // Undoを記録し、アセットをダーティとしてマーク（変更があったことをUnityに通知）
            Undo.RecordObject(card, "Modify CardEntity");
            EditorUtility.SetDirty(card);
            // SaveAssetsを頻繁に呼ぶと重くなる可能性があるため、ここでは呼ばない選択肢もある
            // AssetDatabase.SaveAssets(); 
            // Debug.Log($"CardEntity '{card.Name}' を変更しました");
        }

        // --- アクションボタン（選択、削除） ---
        EditorGUILayout.BeginVertical(GUILayout.Width(100));
        if (GUILayout.Button("Ping", GUILayout.Width(95)))
        {
            Selection.activeObject = card;
            EditorGUIUtility.PingObject(card); // Projectビューでアセットをハイライト
        }

        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f); // 少し薄い赤色
        if (GUILayout.Button("Delete", GUILayout.Width(95)))
        {
            if (EditorUtility.DisplayDialog("削除確認",
                $"'{card.Name}' を削除しますか？\nこの操作は元に戻せません。",
                "削除", "キャンセル"))
            {
                toDelete.Add(card);
            }
        }
        GUI.backgroundColor = originalColor;
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// フッター（カード合計枚数など）を描画します。
    /// </summary>
    private void DrawFooter()
    {
        EditorGUILayout.BeginVertical("box");
        // << ADD: 重複IDがある場合に警告メッセージを表示
        if (duplicateIds.Any())
        {
            EditorGUILayout.HelpBox($"IDが重複しています: {string.Join(", ", duplicateIds)}", MessageType.Warning);
        }
        EditorGUILayout.LabelField($"合計: {cardList.Count} 枚のカード", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
    }

    private void CreateNewCard()
    {
        string createPath = "Assets/Resources/CardEntityList";
        if (!Directory.Exists(createPath))
        {
            Directory.CreateDirectory(createPath);
        }

        CardEntity newCard = CreateInstance<CardEntity>();

        newCard.ID = GetNextAvailableId();
        newCard.Name = $"New Card_{newCard.ID}";
        // ... デフォルト値の設定 ...
        newCard.Type = CardEntity.CardTypeData.Universal;
        newCard.Attribute = AttributeType.Bullet;
        newCard.HitRate = 1.0f;
        newCard.OutputModifier = 1.0f;
        newCard.DefensePenetration = 0.0f;
        newCard.AttackCount = 1;
        newCard.TargetCount = 1;
        newCard.Passive = false;
        newCard.Description = "新しいカードの説明文";

        // ファイルパスが重複しないようにする
        string fileName = $"Card_{newCard.ID}.asset";
        string fullPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(createPath, fileName));

        AssetDatabase.CreateAsset(newCard, fullPath);

        // Addressablesへの登録
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings)
        {
            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(fullPath), settings.DefaultGroup);
            entry.SetLabel("CardEntity", true);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // << MOD: 全データをリロードするのではなく、リストに追加してUIを即時反映させる
        cardList.Add(newCard);
        cardList = cardList.OrderBy(c => c.ID).ToList(); // ソート
        CheckForDuplicateIDs();
        Repaint();

        // 作成したアセットを選択状態にする
        Selection.activeObject = newCard;
        EditorGUIUtility.PingObject(newCard);

        Debug.Log($"新しいCardEntity '{newCard.Name}' を作成しました: {fullPath}");
    }

    private void DeleteCard(CardEntity card)
    {
        string assetPath = AssetDatabase.GetAssetPath(card);
        if (string.IsNullOrEmpty(assetPath)) return;

        // Addressablesから登録解除
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings)
        {
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            settings.RemoveAssetEntry(guid);
        }

        // アセットファイルを削除
        if (AssetDatabase.DeleteAsset(assetPath))
        {
            Debug.Log($"CardEntity '{card.Name}' を削除しました");
        }
        else
        {
            Debug.LogError($"CardEntity '{card.Name}' の削除に失敗しました");
        }
    }

    /// <summary>
    /// 使用されていない最小のIDを1から探索して返します。
    /// </summary>
    private int GetNextAvailableId()
    {
        // リストが空の場合は、最初のIDとして1を返す
        if (cardList == null || !cardList.Any())
        {
            return 1;
        }

        // 既存のIDをHashSetに格納して、高速に存在チェックできるようにする
        var existingIds = new HashSet<int>(cardList.Select(c => c.ID));

        int nextId = 1;
        // ID 1から順に、使用されていない最小のIDを探す
        while (existingIds.Contains(nextId))
        {
            nextId++;
        }

        return nextId;
    }

    /// <summary>
    /// IDの重複をチェックし、結果を `duplicateIds` に格納します。
    /// </summary>
    private void CheckForDuplicateIDs()
    {
        if (cardList == null) return;

        duplicateIds = cardList.GroupBy(c => c.ID)
                               .Where(g => g.Count() > 1)
                               .Select(g => g.Key)
                               .ToHashSet();
    }
}