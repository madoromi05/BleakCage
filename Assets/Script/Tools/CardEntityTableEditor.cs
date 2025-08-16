using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Linq;
using System.IO;
using UnityEditor.AddressableAssets;

/// <summary>
/// CardのGUIでのデータ管理ツール（最終版）
/// </summary>
public class CardEntityTableEditor : EditorWindow
{
    private Vector2 scrollPos;
    private List<CardEntity> cardList;
    private bool isLoading = false;
    private HashSet<int> duplicateIds = new HashSet<int>();

    // 非同期操作のハンドルをメンバー変数として保持
    private AsyncOperationHandle<IList<CardEntity>> loadHandle;

    [MenuItem("Tools/Card Entity Table")]
    public static void OpenWindow()
    {
        GetWindow<CardEntityTableEditor>("Card Table");
    }

    private void OnEnable()
    {
        LoadData();
    }

    private void OnDisable()
    {
        // ウィンドウが無効になる際、実行中の非同期ロード処理があれば必ず解放する
        // これにより、PlayMode移行時などにawaitが迷子になるのを防ぐ
        if (loadHandle.IsValid())
        {
            Addressables.Release(loadHandle);
        }
    }

    /// <summary>
    /// AddressablesからCardEntityを非同期でロードします。
    /// </summary>
    private async void LoadData()
    {
        if (isLoading) return;
        isLoading = true;
        cardList = null;
        Repaint();

        try
        {
            loadHandle = Addressables.LoadAssetsAsync<CardEntity>("CardEntity", null);
            await loadHandle.Task;

            // ロード完了後、ハンドルが無効（OnDisableで解放済み）な場合は処理を中断
            if (!loadHandle.IsValid())
            {
                return;
            }

            if (loadHandle.Status == AsyncOperationStatus.Succeeded)
            {
                cardList = loadHandle.Result?.Where(c => c != null).OrderBy(c => c.ID).ToList() ?? new List<CardEntity>();
                CheckForDuplicateIDs();
            }
            else
            {
                Debug.LogWarning($"CardEntity のロードに失敗しました（Status: {loadHandle.Status}）");
                cardList = new List<CardEntity>();
            }
        }
        catch (System.Exception ex)
        {
            if (ex is System.OperationCanceledException)
            {
                Debug.Log("CardEntityのロードがキャンセルされました。");
            }
            else
            {
                Debug.LogError($"データロード中にエラーが発生しました: {ex.Message}");
            }
            cardList = new List<CardEntity>();
        }
        finally
        {
            isLoading = false;
            Repaint();
        }
    }

    private void OnGUI()
    {
        if (isLoading)
        {
            EditorGUILayout.LabelField("データをロード中...", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        if (cardList == null)
        {
            EditorGUILayout.LabelField("初期化中...", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        DrawToolbar();
        EditorGUILayout.Space();
        DrawCardTable();
        DrawFooter();
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

        DrawHeader();

        List<CardEntity> toDelete = new List<CardEntity>();

        // リストのコピーを作成してイテレーションすることで、編集中にリストが変更されてもエラーを防ぐ
        foreach (var card in new List<CardEntity>(cardList))
        {
            if (card == null) continue;
            DrawCardRow(card, toDelete);
        }

        if (toDelete.Any())
        {
            foreach (var card in toDelete)
            {
                DeleteCardAsset(card);
            }
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

        Color originalColor = GUI.backgroundColor;
        if (duplicateIds.Contains(card.ID))
        {
            GUI.backgroundColor = Color.yellow;
        }

        EditorGUI.BeginChangeCheck();

        int newId = EditorGUILayout.IntField(card.ID, GUILayout.Width(40));
        if (newId != card.ID)
        {
            card.ID = newId;
            cardList = cardList.OrderBy(c => c.ID).ToList();
            CheckForDuplicateIDs();
        }
        GUI.backgroundColor = originalColor;

        card.Name = EditorGUILayout.TextField(card.Name, GUILayout.Width(120));
        card.Type = (CardEntity.CardTypeData)EditorGUILayout.EnumPopup(card.Type, GUILayout.Width(80));

        string weaponIds = string.Join(",", card.EquipableWeaponID ?? new int[0]);
        string newWeaponIds = EditorGUILayout.TextField(weaponIds, GUILayout.Width(150));
        if (newWeaponIds != weaponIds)
        {
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

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(card, "Modify CardEntity");
            EditorUtility.SetDirty(card);
        }

        EditorGUILayout.BeginVertical(GUILayout.Width(100));
        if (GUILayout.Button("Ping", GUILayout.Width(95)))
        {
            Selection.activeObject = card;
            EditorGUIUtility.PingObject(card);
        }

        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
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
        newCard.Type = CardEntity.CardTypeData.Universal;
        newCard.Attribute = AttributeType.Bullet;
        newCard.HitRate = 1.0f;
        newCard.OutputModifier = 1.0f;
        newCard.DefensePenetration = 0.0f;
        newCard.AttackCount = 1;
        newCard.TargetCount = 1;
        newCard.Passive = false;
        newCard.Description = "新しいカードの説明文";

        string fileName = $"Card_{newCard.ID}.asset";
        string fullPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(createPath, fileName));

        AssetDatabase.CreateAsset(newCard, fullPath);

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings)
        {
            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(fullPath), settings.DefaultGroup);
            entry.SetLabel("CardEntity", true);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        cardList.Add(newCard);
        cardList = cardList.OrderBy(c => c.ID).ToList();
        CheckForDuplicateIDs();
        Repaint();

        Selection.activeObject = newCard;
        EditorGUIUtility.PingObject(newCard);

        Debug.Log($"新しいCardEntity '{newCard.Name}' を作成しました: {fullPath}");
    }

    private void DeleteCardAsset(CardEntity card)
    {
        string assetPath = AssetDatabase.GetAssetPath(card);
        if (string.IsNullOrEmpty(assetPath)) return;

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings)
        {
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            settings.RemoveAssetEntry(guid);
        }

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
        if (cardList == null || !cardList.Any())
        {
            return 1;
        }

        var existingIds = new HashSet<int>(cardList.Select(c => c.ID));
        int nextId = 1;
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