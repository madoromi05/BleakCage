//using UnityEngine;
//using UnityEditor;
//using System.Collections.Generic;
//using UnityEngine.AddressableAssets;
//using UnityEngine.ResourceManagement.AsyncOperations;
//using System.Linq;
//using System.IO;
//using UnityEditor.AddressableAssets;

///// <summary>
///// CardのGUIでのデータ管理ツール（ツリービュー版）
///// </summary>
//public class CardEntityTableEditor : EditorWindow
//{
//    private Vector2 scrollPos;
//    private List<CardEntity> allCardList;
//    private bool isLoading = false;
//    private HashSet<int> duplicateIds = new HashSet<int>();
//    private AsyncOperationHandle<IList<CardEntity>> loadHandle;

//    // Foldoutの状態を保持
//    private Dictionary<string, bool> categoryFoldouts = new Dictionary<string, bool>();

//    [MenuItem("Tools/Card Entity Table")]
//    public static void OpenWindow()
//    {
//        GetWindow<CardEntityTableEditor>("Card Table");
//    }

//    private void OnEnable()
//    {
//        LoadData();
//    }

//    private void OnDisable()
//    {
//        if (loadHandle.IsValid())
//        {
//            Addressables.Release(loadHandle);
//        }
//    }

//    private async void LoadData()
//    {
//        if (isLoading) return;
//        isLoading = true;
//        allCardList = null;
//        Repaint();

//        try
//        {
//            loadHandle = Addressables.LoadAssetsAsync<CardEntity>("CardEntity", null);
//            await loadHandle.Task;

//            if (!loadHandle.IsValid()) return;

//            if (loadHandle.Status == AsyncOperationStatus.Succeeded)
//            {
//                allCardList = loadHandle.Result?.Where(c => c != null).OrderBy(c => c.ID).ToList() ?? new List<CardEntity>();
//                CheckForDuplicateIDs();
//            }
//            else
//            {
//                Debug.LogWarning($"CardEntity のロードに失敗しました（Status: {loadHandle.Status}）");
//                allCardList = new List<CardEntity>();
//            }
//        }
//        catch (System.Exception ex)
//        {
//            if (ex is System.OperationCanceledException)
//            {
//                Debug.Log("CardEntityのロードがキャンセルされました。");
//            }
//            else
//            {
//                Debug.LogError($"データロード中にエラーが発生しました: {ex.Message}");
//            }
//            allCardList = new List<CardEntity>();
//        }
//        finally
//        {
//            isLoading = false;
//            Repaint();
//        }
//    }

//    private void OnGUI()
//    {
//        if (isLoading)
//        {
//            EditorGUILayout.LabelField("データをロード中...", EditorStyles.centeredGreyMiniLabel);
//            return;
//        }

//        if (allCardList == null)
//        {
//            EditorGUILayout.LabelField("初期化中...", EditorStyles.centeredGreyMiniLabel);
//            return;
//        }

//        DrawToolbar();
//        EditorGUILayout.Space();
//        DrawCardTable();
//        DrawFooter();
//    }

//    private void DrawToolbar()
//    {
//        EditorGUILayout.BeginVertical("box");

//        if (GUILayout.Button("Reload Cards"))
//        {
//            LoadData();
//        }

//        EditorGUILayout.Space();

//        if (GUILayout.Button("新規作成 (汎用)", GUILayout.Width(150)))
//        {
//            // ▼▼▼【変更】CreateNewCardの引数を(type, charID, weaponID)に ▼▼▼
//            CreateNewCard(CardTypeData.Universal, 0, 0);
//            // ▲▲▲【変更】▲▲▲
//        }

//        EditorGUILayout.EndVertical();
//    }

//    /// <summary>
//    /// カードデータの一覧テーブル（ツリービュー）を描画します。
//    /// </summary>
//    private void DrawCardTable()
//    {
//        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

//        DrawHeader();

//        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

//        List<CardEntity> toDelete = new List<CardEntity>();

//        if (allCardList == null || !allCardList.Any())
//        {
//            EditorGUILayout.LabelField("データがありません。");
//            EditorGUILayout.EndScrollView();
//            EditorGUILayout.EndVertical();
//            return;
//        }

//        // --- 階層1: Universal ---
//        var universalCards = allCardList.Where(c => c.Type == CardTypeData.Universal).ToList();
//        DrawCategoryFoldout("Universal", universalCards, toDelete, () => CreateNewCard(CardTypeData.Universal, 0, 0));

//        // ▼▼▼【変更】Weapon カテゴリの階層化 ▼▼▼
//        // --- 階層1: Weapon ---
//        var weaponCards = allCardList.Where(c => c.Type == CardTypeData.Weapon).ToList();
//        string weaponKey = "Weapon";
//        EnsureFoldoutKey(weaponKey, false);

//        EditorGUILayout.BeginHorizontal();
//        categoryFoldouts[weaponKey] = EditorGUILayout.Foldout(categoryFoldouts[weaponKey], $"Weapon ({weaponCards.Count})", true, EditorStyles.foldoutHeader);
//        if (GUILayout.Button("+", GUILayout.Width(30)))
//        {
//            // デフォルトでWeaponID 1 のカードとして作成
//            CreateNewCard(CardTypeData.Weapon, 0, 1);
//        }
//        EditorGUILayout.EndHorizontal();

//        if (categoryFoldouts[weaponKey])
//        {
//            // --- 階層2: EquipableWeaponID ごとにグループ化 ---
//            var groupedByWeapon = weaponCards.GroupBy(c => c.EquipableWeaponID).OrderBy(g => g.Key);

//            EditorGUI.indentLevel++;
//            foreach (var group in groupedByWeapon)
//            {
//                int weaponID = group.Key;
//                string subKey = $"WeaponID_{weaponID}";
//                var groupCards = group.ToList();

//                DrawCategoryFoldout(subKey, $"Weapon ID: {weaponID}", groupCards, toDelete, () => CreateNewCard(CardTypeData.Weapon, 0, weaponID));
//            }
//            EditorGUI.indentLevel--;
//        }
//        // ▲▲▲【変更】▲▲▲

//        // --- 階層1: Character ---
//        var characterCards = allCardList.Where(c => c.Type == CardTypeData.Character).ToList();
//        string charKey = "Character";
//        EnsureFoldoutKey(charKey, false);

//        EditorGUILayout.BeginHorizontal();
//        categoryFoldouts[charKey] = EditorGUILayout.Foldout(categoryFoldouts[charKey], $"Character ({characterCards.Count})", true, EditorStyles.foldoutHeader);
//        if (GUILayout.Button("+", GUILayout.Width(30)))
//        {
//            CreateNewCard(CardTypeData.Character, 1, 0); // デフォルトCharID 1
//        }
//        EditorGUILayout.EndHorizontal();

//        if (categoryFoldouts[charKey])
//        {
//            // --- 階層2: CharacterID ごとにグループ化 ---
//            var groupedByChar = characterCards.GroupBy(c => c.CharacterID).OrderBy(g => g.Key);

//            EditorGUI.indentLevel++;
//            foreach (var group in groupedByChar)
//            {
//                int charID = group.Key;
//                string subKey = $"CharID_{charID}";
//                var groupCards = group.ToList();

//                DrawCategoryFoldout(subKey, $"Character ID: {charID}", groupCards, toDelete, () => CreateNewCard(CardTypeData.Character, charID, 0));
//            }
//            EditorGUI.indentLevel--;
//        }

//        if (toDelete.Any())
//        {
//            foreach (var card in toDelete)
//            {
//                DeleteCardAsset(card);
//            }
//            allCardList.RemoveAll(toDelete.Contains);
//            CheckForDuplicateIDs();
//        }

//        EditorGUILayout.EndScrollView();
//        EditorGUILayout.EndVertical();
//    }

//    private void EnsureFoldoutKey(string key, bool defaultState = true)
//    {
//        if (!categoryFoldouts.ContainsKey(key))
//        {
//            categoryFoldouts.Add(key, defaultState);
//        }
//    }

//    private void DrawCategoryFoldout(string key, string displayName, List<CardEntity> cards, List<CardEntity> toDelete, System.Action onAddButtonPress)
//    {
//        EnsureFoldoutKey(key, false);

//        EditorGUILayout.BeginHorizontal();
//        categoryFoldouts[key] = EditorGUILayout.Foldout(categoryFoldouts[key], $"{displayName} ({cards.Count})", true, EditorStyles.foldoutHeader);

//        if (GUILayout.Button("+", GUILayout.Width(30)))
//        {
//            onAddButtonPress?.Invoke();
//        }
//        EditorGUILayout.EndHorizontal();

//        if (categoryFoldouts[key])
//        {
//            EditorGUI.indentLevel++;
//            foreach (var card in cards)
//            {
//                if (card == null) continue;
//                DrawCardRow(card, toDelete);
//            }
//            EditorGUI.indentLevel--;
//        }
//    }

//    private void DrawCategoryFoldout(string key, List<CardEntity> cards, List<CardEntity> toDelete, System.Action onAddButtonPress)
//    {
//        DrawCategoryFoldout(key, key, cards, toDelete, onAddButtonPress);
//    }


//    /// <summary>
//    /// テーブルのヘッダーを描画します。
//    /// </summary>
//    private void DrawHeader()
//    {
//        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
//        GUILayout.Label("ID", GUILayout.Width(40));
//        GUILayout.Label("名前", GUILayout.Width(120));
//        GUILayout.Label("Type", GUILayout.Width(80));
//        GUILayout.Label("武器ID", GUILayout.Width(100)); // (Weapon専用)
//        GUILayout.Label("キャラID", GUILayout.Width(60)); // (Character専用)
//        GUILayout.Label("攻撃属性", GUILayout.Width(80));
//        GUILayout.Label("出力調整", GUILayout.Width(80));
//        GUILayout.Label("命中率", GUILayout.Width(80));
//        GUILayout.Label("防御貫通率", GUILayout.Width(80));
//        GUILayout.Label("攻撃回数", GUILayout.Width(80));
//        GUILayout.Label("攻撃対数", GUILayout.Width(80));
//        GUILayout.Label("Passive", GUILayout.Width(60));
//        GUILayout.Label("Icon", GUILayout.Width(60));
//        GUILayout.Label("説明文", GUILayout.ExpandWidth(true));
//        GUILayout.Label("Actions", GUILayout.Width(100));
//        EditorGUILayout.EndHorizontal();
//    }

//    /// <summary>
//    /// カード1枚分の行を描画します。
//    /// </summary>
//    private void DrawCardRow(CardEntity card, List<CardEntity> toDelete)
//    {
//        EditorGUILayout.BeginHorizontal();

//        Color originalColor = GUI.backgroundColor;
//        if (duplicateIds.Contains(card.ID))
//        {
//            GUI.backgroundColor = Color.yellow;
//        }

//        EditorGUI.BeginChangeCheck();

//        int newId = EditorGUILayout.IntField(card.ID, GUILayout.Width(40));
//        if (newId != card.ID)
//        {
//            card.ID = newId;
//            allCardList = allCardList.OrderBy(c => c.ID).ToList();
//            CheckForDuplicateIDs();
//        }
//        GUI.backgroundColor = originalColor;

//        card.Name = EditorGUILayout.TextField(card.Name, GUILayout.Width(120));
//        card.Type = (CardTypeData)EditorGUILayout.EnumPopup(card.Type, GUILayout.Width(80));

//        if (card.Type == CardTypeData.Weapon)
//        {
//            card.EquipableWeaponID = EditorGUILayout.IntField(card.EquipableWeaponID, GUILayout.Width(100));
//        }
//        else
//        {
//            EditorGUILayout.LabelField("-", GUILayout.Width(100));
//        }

//        if (card.Type == CardTypeData.Character)
//        {
//            card.CharacterID = EditorGUILayout.IntField(card.CharacterID, GUILayout.Width(60));
//        }
//        else
//        {
//            EditorGUILayout.LabelField("-", GUILayout.Width(60));
//        }

//        card.Attribute = (AttributeType)EditorGUILayout.EnumPopup(card.Attribute, GUILayout.Width(80));
//        card.OutputModifier = EditorGUILayout.FloatField(card.OutputModifier, GUILayout.Width(80));
//        card.HitRate = EditorGUILayout.FloatField(card.HitRate, GUILayout.Width(80));
//        card.DefensePenetration = EditorGUILayout.FloatField(card.DefensePenetration, GUILayout.Width(80));
//        card.AttackCount = EditorGUILayout.IntField(card.AttackCount, GUILayout.Width(80));
//        card.TargetCount = EditorGUILayout.IntField(card.TargetCount, GUILayout.Width(80));
//        card.Passive = EditorGUILayout.Toggle(card.Passive, GUILayout.Width(60));
//        card.Icon = (Sprite)EditorGUILayout.ObjectField(card.Icon, typeof(Sprite), false, GUILayout.Width(60));
//        card.Description = EditorGUILayout.TextArea(card.Description, GUILayout.ExpandWidth(true), GUILayout.Height(40));

//        if (EditorGUI.EndChangeCheck())
//        {
//            Undo.RecordObject(card, "Modify CardEntity");
//            EditorUtility.SetDirty(card);
//            // タイプやID変更でツリーの所属が変わる可能性があるため、Repaint
//            Repaint();
//        }

//        EditorGUILayout.BeginVertical(GUILayout.Width(100));
//        if (GUILayout.Button("Ping", GUILayout.Width(95)))
//        {
//            Selection.activeObject = card;
//            EditorGUIUtility.PingObject(card);
//        }

//        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
//        if (GUILayout.Button("Delete", GUILayout.Width(95)))
//        {
//            if (EditorUtility.DisplayDialog("削除確認",
//                $"'{card.Name}' を削除しますか？\nこの操作は元に戻せません。",
//                "削除", "キャンセル"))
//            {
//                toDelete.Add(card);
//            }
//        }
//        GUI.backgroundColor = originalColor;
//        EditorGUILayout.EndVertical();

//        EditorGUILayout.EndHorizontal();
//    }

//    private void DrawFooter()
//    {
//        EditorGUILayout.BeginVertical("box");
//        if (duplicateIds.Any())
//        {
//            EditorGUILayout.HelpBox($"IDが重複しています: {string.Join(", ", duplicateIds)}", MessageType.Warning);
//        }

//        if (allCardList != null)
//        {
//            EditorGUILayout.LabelField($"合計: {allCardList.Count} 枚のカード", EditorStyles.miniLabel);
//        }
//        EditorGUILayout.EndVertical();
//    }

//    // ▼▼▼【変更】CreateNewCard のシグネチャとロジック ▼▼▼
//    private void CreateNewCard(CardTypeData type, int characterID, int weaponID)
//    {
//        string baseCreatePath = "Assets/Resources/CardEntityList";
//        if (!Directory.Exists(baseCreatePath))
//        {
//            Directory.CreateDirectory(baseCreatePath);
//        }

//        CardEntity newCard = CreateInstance<CardEntity>();

//        newCard.ID = GetNextAvailableId();
//        newCard.Name = $"New Card_{newCard.ID}";

//        newCard.Type = type;

//        // フォルダパスとデフォルトIDを設定
//        string subFolder = type.ToString();
//        if (type == CardTypeData.Character)
//        {
//            newCard.CharacterID = characterID;
//            subFolder = Path.Combine(subFolder, $"CharID_{characterID}");
//        }
//        else if (type == CardTypeData.Weapon)
//        {
//            newCard.EquipableWeaponID = weaponID;
//            subFolder = Path.Combine(subFolder, $"WeaponID_{weaponID}");
//        }

//        newCard.Attribute = AttributeType.Bullet;
//        newCard.HitRate = 1.0f;
//        newCard.OutputModifier = 1.0f;
//        newCard.DefensePenetration = 0.0f;
//        newCard.AttackCount = 1;
//        newCard.TargetCount = 1;
//        newCard.EquipableWeaponID = (type == CardTypeData.Weapon) ? weaponID : 0;
//        newCard.CharacterID = (type == CardTypeData.Character) ? characterID : 0;
//        newCard.Description = "新しいカードの説明文";
//        newCard.Icon = null;

//        string createPath = Path.Combine(baseCreatePath, subFolder);
//        if (!Directory.Exists(createPath))
//        {
//            Directory.CreateDirectory(createPath);
//        }

//        string fileName = $"Card_{newCard.ID}.asset";
//        string fullPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(createPath, fileName));

//        AssetDatabase.CreateAsset(newCard, fullPath);

//        var settings = AddressableAssetSettingsDefaultObject.Settings;
//        if (settings)
//        {
//            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(fullPath), settings.DefaultGroup);
//            entry.SetLabel("CardEntity", true);
//        }

//        AssetDatabase.SaveAssets();
//        AssetDatabase.Refresh();

//        allCardList.Add(newCard);
//        allCardList = allCardList.OrderBy(c => c.ID).ToList();

//        CheckForDuplicateIDs();
//        Repaint();

//        // 新規作成したカテゴリを開いておく
//        if (type == CardTypeData    .Character)
//        {
//            EnsureFoldoutKey("Character", true);
//            EnsureFoldoutKey($"CharID_{characterID}", true);
//        }
//        else if (type == CardTypeData.Weapon)
//        {
//            EnsureFoldoutKey("Weapon", true);
//            EnsureFoldoutKey($"WeaponID_{weaponID}", true);
//        }
//        else
//        {
//            EnsureFoldoutKey("Universal", true);
//        }

//        Selection.activeObject = newCard;
//        EditorGUIUtility.PingObject(newCard);

//        Debug.Log($"新しいCardEntity '{newCard.Name}' を作成しました: {fullPath}");
//    }
//    // ▲▲▲【変更】▲▲▲

//    private void DeleteCardAsset(CardEntity card)
//    {
//        string assetPath = AssetDatabase.GetAssetPath(card);
//        if (string.IsNullOrEmpty(assetPath)) return;

//        var settings = AddressableAssetSettingsDefaultObject.Settings;
//        if (settings)
//        {
//            var guid = AssetDatabase.AssetPathToGUID(assetPath);
//            settings.RemoveAssetEntry(guid);
//        }

//        if (AssetDatabase.DeleteAsset(assetPath))
//        {
//            Debug.Log($"CardEntity '{card.Name}' を削除しました");
//        }
//        else
//        {
//            Debug.LogError($"CardEntity '{card.Name}' の削除に失敗しました");
//        }
//    }

//    private int GetNextAvailableId()
//    {
//        if (allCardList == null || !allCardList.Any())
//        {
//            return 1;
//        }

//        var existingIds = new HashSet<int>(allCardList.Select(c => c.ID));
//        int nextId = 1;
//        while (existingIds.Contains(nextId))
//        {
//            nextId++;
//        }
//        return nextId;
//    }

//    private void CheckForDuplicateIDs()
//    {
//        if (allCardList == null) return;

//        duplicateIds = allCardList.GroupBy(c => c.ID)
//                                .Where(g => g.Count() > 1)
//                                .Select(g => g.Key)
//                                .ToHashSet();
//    }
//}