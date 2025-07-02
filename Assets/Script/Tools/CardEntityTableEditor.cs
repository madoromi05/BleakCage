using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

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

    private void LoadData()
    {
        // CardEntity をすべて検索
        string[] guids = AssetDatabase.FindAssets("t:CardEntity");
        cardList = guids
            .Select(guid => AssetDatabase.LoadAssetAtPath<CardEntity>(AssetDatabase.GUIDToAssetPath(guid)))
            .ToList();
    }

    // 値参照コマンド
    private string ResolveTemplate(string template, CardEntity card)
    {
        return template
            .Replace("{Type}", card.cardType.ToString())
            .Replace("{Attribute}", card.CardAttribute.ToString())
            .Replace("{Power}", card.basePower.ToString())
            .Replace("\\", "\n");
    }

    private void OnGUI()
    {
        if (cardList == null)
        {
            LoadData();
        }

        if (GUILayout.Button("Reload Cards"))
        {
            LoadData();
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // ヘッダー行(項目の位置を直接指定)
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("ID", GUILayout.Width(30));
        GUILayout.Label("Name", GUILayout.Width(120));
        GUILayout.Label("CardType", GUILayout.Width(80));
        GUILayout.Label("属性", GUILayout.Width(80));
        GUILayout.Label("力", GUILayout.Width(50));
        GUILayout.Label("画像", GUILayout.Width(60));
        GUILayout.Label("説明", GUILayout.Width(500));
        EditorGUILayout.EndHorizontal();

        // 各行：CardEntity
        foreach (var card in cardList)
        {
            //項目を横並びにに表示
            EditorGUILayout.BeginHorizontal();

            // 変更開始検知
            EditorGUI.BeginChangeCheck();

            // 変更箇所の位置調整
            card.cardId = EditorGUILayout.IntField(card.cardId, GUILayout.Width(30));
            card.cardName = EditorGUILayout.TextField(card.cardName, GUILayout.Width(120));
            card.cardType = (CardEntity.CardType)EditorGUILayout.EnumPopup(card.cardType, GUILayout.Width(80));
            card.CardAttribute = (CardEntity.Attribute)EditorGUILayout.EnumPopup(card.CardAttribute, GUILayout.Width(80));
            card.basePower = EditorGUILayout.IntField(card.basePower, GUILayout.Width(50));
            card.CardIcon = (Sprite)EditorGUILayout.ObjectField(card.CardIcon, typeof(Sprite), false, GUILayout.Width(60));
            card.CardDescription = EditorGUILayout.TextField(card.CardDescription, GUILayout.Width(500));

            string resolvedDescription = ResolveTemplate(card.CardDescription, card);
            EditorGUILayout.LabelField(resolvedDescription, GUILayout.Width(300));

            // 変更があったら保存処理
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(card, "Auto Save CardEntity");
                EditorUtility.SetDirty(card);                                       // どのカードを変更したのか
                AssetDatabase.SaveAssets();　                                       // アセットに保存
                AssetDatabase.Refresh();                                            // 反映させるためのリフレッシュ
                Debug.Log($"CardEntity '{card.cardName}' を自動保存しました");
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }
}
