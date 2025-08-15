using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Linq;
using System.IO;

/// <summary>
/// CardのGUIでのデータ管理ツール
/// </summary>
public class CardEntityTableEditor : EditorWindow
{
    private Vector2 scrollPos;
    private List<CardEntity> cardList;

    [MenuItem("Tools/Card Entity Table")]
    public static void OpenWindow()
    {
        GetWindow<CardEntityTableEditor>("Card Table");
    }

    private void OnEnable()
    {
        LoadData();
    }

    private async void LoadData()
    {
        cardList = new List<CardEntity>();

        AsyncOperationHandle<IList<CardEntity>> handle =
            Addressables.LoadAssetsAsync<CardEntity>("CardEntity", null);

        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            cardList = handle.Result?.Where(c => c != null).ToList() ?? new List<CardEntity>();
            Repaint(); // << 再描画して即座に反映
        }
        else
        {
            Debug.LogWarning($"CardEntity のロードに失敗しました（Status: {handle.Status}）");
        }
    }

    private void OnGUI()
    {
        if (cardList == null)
        {
            LoadData();
            return;
        }

        EditorGUILayout.BeginVertical("box");

        if (GUILayout.Button("Reload Cards"))
        {
            LoadData();
        }

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("新規作成", GUILayout.Width(80)))
        {
            CreateNewCard();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        cardList = cardList.OrderBy(c => c.ID).ToList();

        // ヘッダー行
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("ID", GUILayout.Width(30));
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
        GUILayout.Label("説明文", GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();

        List<CardEntity> toDelete = new List<CardEntity>();

        foreach (var card in cardList)
        {
            if (card == null) continue;

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();

            card.ID = EditorGUILayout.IntField(card.ID, GUILayout.Width(30));
            card.Name = EditorGUILayout.TextField(card.Name, GUILayout.Width(120));
            card.Type = (CardEntity.CardTypeData)EditorGUILayout.EnumPopup(card.Type, GUILayout.Width(80));

            // 装備可能武器ID入力欄
            string weaponIds = string.Join(",", card.EquipableWeaponID ?? new int[0]);
            string newWeaponIds = EditorGUILayout.TextField(weaponIds, GUILayout.Width(150));

            if (newWeaponIds != weaponIds)
            {
                try
                {
                    card.EquipableWeaponID = newWeaponIds
                        .Split(',')
                        .Select(s => int.Parse(s.Trim()))
                        .ToArray();
                }
                catch
                {
                    Debug.LogError("装備可能武器IDの入力に誤りがあります。整数をカンマ区切りで入力してください。");
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
            card.Description = EditorGUILayout.TextArea(card.Description, GUILayout.Width(600), GUILayout.Height(40));

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(card, "Auto Save CardEntity");
                EditorUtility.SetDirty(card);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"CardEntity '{card.Name}' を自動保存しました");
            }

            EditorGUILayout.BeginHorizontal(GUILayout.Width(100));

            if (GUILayout.Button("選択", GUILayout.Width(45)))
            {
                Selection.activeObject = card;
                EditorGUIUtility.PingObject(card);
            }

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("削除", GUILayout.Width(45)))
            {
                if (EditorUtility.DisplayDialog("削除確認",
                    $"'{card.Name}' を削除しますか？\nこの操作は元に戻せません。",
                    "削除", "キャンセル"))
                {
                    toDelete.Add(card);
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();
        }

        foreach (var card in toDelete)
        {
            DeleteCard(card);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"合計: {cardList.Count} 枚のカード", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
    }

    private void CreateNewCard()
    {
        string createPath = "Assets/AddressableAssets/CardEntityList";
        if (!Directory.Exists(createPath))
        {
            Directory.CreateDirectory(createPath);
        }

        CardEntity newCard = ScriptableObject.CreateInstance<CardEntity>();

        newCard.ID = GetNextAvailableId();
        newCard.Name = $"Card_{newCard.ID}";
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
        string fullPath = Path.Combine(createPath, fileName);

        AssetDatabase.CreateAsset(newCard, fullPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Addressablesラベル追加（ラベル名: CardEntity）
        var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
        var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(fullPath), settings.DefaultGroup);
        entry.SetLabel("CardEntity", true);

        LoadData();
        Selection.activeObject = newCard;
        EditorGUIUtility.PingObject(newCard);

        Debug.Log($"新しいCardEntity '{newCard.Name}' を作成しました: {fullPath}");
    }

    private void DeleteCard(CardEntity card)
    {
        string assetPath = AssetDatabase.GetAssetPath(card);
        if (string.IsNullOrEmpty(assetPath)) return;

        var guid = AssetDatabase.AssetPathToGUID(assetPath);
        var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
        settings.RemoveAssetEntry(guid);

        if (AssetDatabase.DeleteAsset(assetPath))
        {
            Debug.Log($"CardEntity '{card.Name}' を削除しました");
            LoadData();
        }
        else
        {
            Debug.LogError($"CardEntity '{card.Name}' の削除に失敗しました");
        }
    }

    private int GetNextAvailableId()
    {
        if (cardList == null || cardList.Count == 0 || !cardList.Any(c => c != null))
            return 1;

        return cardList.Where(c => c != null).Max(c => c.ID) + 1;
    }
}
