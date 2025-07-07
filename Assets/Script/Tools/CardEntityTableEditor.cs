using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

/// <summary>
/// CardのGUIでのデータ管理ツール
/// </summary>
public class CardEntityTableEditor : EditorWindow
{
    private Vector2 scrollPos;
    private List<CardEntity> cardList;
    private string newCardName = "Card";
    private string savePath = "Assets/Resources/CardEntityList";

    [MenuItem("Tools/Card Entity Table")]
    public static void OpenWindow()
    {
        GetWindow<CardEntityTableEditor>("Card Table");
    }

    private void OnEnable()
    {
        LoadData();
    }

    private void LoadData()
    {
        string[] guids = AssetDatabase.FindAssets("t:CardEntity", new string[] { savePath });
        cardList = guids
            .Select(guid => AssetDatabase.LoadAssetAtPath<CardEntity>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(card => card != null)
            .ToList();
    }

    private string GetCardTypeDisplayName(CardEntity.CardTypeData cardType)
    {
        switch (cardType)
        {
            case CardEntity.CardTypeData.Character:
                return "キャラ付き";
            case CardEntity.CardTypeData.Weapon:
                return "武器付き";
            case CardEntity.CardTypeData.Universal:
                return "汎用";
            default:
                return cardType.ToString();
        }
    }

    private void OnGUI()
    {
        if (cardList == null)
        {
            LoadData();
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

        // ヘッダー行
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("ID", GUILayout.Width(30));
        GUILayout.Label("Name", GUILayout.Width(120));
        GUILayout.Label("Type", GUILayout.Width(80));
        GUILayout.Label("Attribute", GUILayout.Width(80));
        GUILayout.Label("HitRate", GUILayout.Width(80));
        GUILayout.Label("OutputMod", GUILayout.Width(80));
        GUILayout.Label("Penetration", GUILayout.Width(80));
        GUILayout.Label("AttackCount", GUILayout.Width(80));
        GUILayout.Label("TargetCount", GUILayout.Width(80));
        GUILayout.Label("Passive", GUILayout.Width(60));
        GUILayout.Label("Icon", GUILayout.Width(60));
        GUILayout.Label("Description", GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();

        List<CardEntity> toDelete = new List<CardEntity>();

        foreach (var card in cardList)
        {
            if (card == null) continue;

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();

            card.CardId = EditorGUILayout.IntField(card.CardId, GUILayout.Width(30));
            card.CardName = EditorGUILayout.TextField(card.CardName, GUILayout.Width(120));
            card.CardType = (CardEntity.CardTypeData)EditorGUILayout.EnumPopup(card.CardType, GUILayout.Width(80));
            card.CardAttribute = (AttackAttributeType)EditorGUILayout.EnumPopup(card.CardAttribute, GUILayout.Width(80));
            card.CardHitRate = EditorGUILayout.FloatField(card.CardHitRate, GUILayout.Width(80));
            card.CardOutputModifier = EditorGUILayout.FloatField(card.CardOutputModifier, GUILayout.Width(80));
            card.CardDefensePenetration = EditorGUILayout.FloatField(card.CardDefensePenetration, GUILayout.Width(80));
            card.CardAttackCount = EditorGUILayout.IntField(card.CardAttackCount, GUILayout.Width(80));
            card.CardTargetCount = EditorGUILayout.IntField(card.CardTargetCount, GUILayout.Width(80));
            card.CardPassive = EditorGUILayout.Toggle(card.CardPassive, GUILayout.Width(60));
            card.CardIcon = (Sprite)EditorGUILayout.ObjectField(card.CardIcon, typeof(Sprite), false, GUILayout.Width(60));
            card.CardDescription = EditorGUILayout.TextArea(card.CardDescription, GUILayout.Width(600), GUILayout.Height(40));

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(card, "Auto Save CardEntity");
                EditorUtility.SetDirty(card);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"CardEntity '{card.CardName}' を自動保存しました");
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
                    $"'{card.CardName}' を削除しますか？\nこの操作は元に戻せません。",
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
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
            AssetDatabase.Refresh();
        }

        CardEntity newCard = ScriptableObject.CreateInstance<CardEntity>();

        newCard.CardId = GetNextAvailableId();
        newCard.CardName = "New Card";
        newCard.CardType = CardEntity.CardTypeData.Universal;
        newCard.CardAttribute = AttackAttributeType.Bullet;
        newCard.CardHitRate = 1.0f;
        newCard.CardOutputModifier = 1.0f;
        newCard.CardDefensePenetration = 0.0f;
        newCard.CardAttackCount = 1;
        newCard.CardTargetCount = 1;
        newCard.CardPassive = false;
        newCard.CardDescription = "新しいカードの説明文";

        string fileName = $"{newCardName}_{newCard.CardId}.asset";
        string filePath = Path.Combine(savePath, fileName);
        int counter = 1;

        while (File.Exists(filePath))
        {
            fileName = $"{newCardName}_{newCard.CardId}_{counter}.asset";
            filePath = Path.Combine(savePath, fileName);
            counter++;
        }

        AssetDatabase.CreateAsset(newCard, filePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        LoadData();
        Selection.activeObject = newCard;
        EditorGUIUtility.PingObject(newCard);

        Debug.Log($"新しいCardEntity '{newCard.CardName}' を作成しました: {filePath}");
    }

    private void DeleteCard(CardEntity card)
    {
        if (card == null) return;

        string assetPath = AssetDatabase.GetAssetPath(card);
        if (string.IsNullOrEmpty(assetPath)) return;

        if (AssetDatabase.DeleteAsset(assetPath))
        {
            Debug.Log($"CardEntity '{card.CardName}' を削除しました");
            LoadData();
        }
        else
        {
            Debug.LogError($"CardEntity '{card.CardName}' の削除に失敗しました");
        }
    }

    private int GetNextAvailableId()
    {
        if (cardList == null || cardList.Count == 0)
            return 1;

        int maxId = cardList.Where(c => c != null).Max(c => c.CardId);
        return maxId + 1;
    }
}
