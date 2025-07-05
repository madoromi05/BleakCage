using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// CardのGUIでのデータ管理ツール
/// </summary>
public class CardEntityTableEditor : EditorWindow
{
    private Vector2 scrollPos;
    private List<CardEntity> cardList;
    private string newCardName = "Card";
    private string savePath = "Assets/Resources/CardEntityList";
    private bool showResolvedDescription = true; // 解決済み説明文を表示するかどうか

    // プレビュー用の変数
    private CardEntity previewCard;
    private bool showPreview = false;
    private Vector2 previewScrollPos;

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
        // Assets/Resources/CardEntityList中のCardEntity をすべて検索
        string[] guids = AssetDatabase.FindAssets("t:CardEntity", new string[] { "Assets/Resources/CardEntityList" });
        cardList = guids
            .Select(guid => AssetDatabase.LoadAssetAtPath<CardEntity>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(card => card != null) // nullチェック
            .ToList();
    }

    /// <summary>
    /// カードタイプの日本語表示名を取得
    /// </summary>
    private string GetCardTypeDisplayName(CardEntity.CardType cardType)
    {
        switch (cardType)
        {
            case CardEntity.CardType.Character:
                return "キャラ付き";
            case CardEntity.CardType.Weapon:
                return "武器付き";
            case CardEntity.CardType.Universal:
                return "汎用";
            default:
                return cardType.ToString();
        }
    }

    /// <summary>
    /// 属性の日本語表示名を取得
    /// </summary>
    private string GetAttributeDisplayName(CardEntity.Attribute attribute)
    {
        switch (attribute)
        {
            case CardEntity.Attribute.Slash:
                return "斬";
            case CardEntity.Attribute.Blunt:
                return "鈍";
            case CardEntity.Attribute.Pierce:
                return "突";
            case CardEntity.Attribute.Bullet:
                return "弾";
            default:
                return attribute.ToString();
        }
    }

    /// <summary>
    /// 説明文のプレースホルダーを実際の値に置換
    /// </summary>
    private string GetResolvedDescription(CardEntity card)
    {
        if (string.IsNullOrEmpty(card.CardDescription))
            return "";

        return card.CardDescription
            .Replace("{Type}", GetCardTypeDisplayName(card.cardType))
            .Replace("{Attribute}", GetAttributeDisplayName(card.CardAttribute))
            .Replace("{Power}", card.basePower.ToString());
    }

    /// <summary>
    /// カードプレビューの表示
    /// </summary>
    private void DrawCardPreview(CardEntity card)
    {
        if (card == null) return;

        EditorGUILayout.BeginVertical("box", GUILayout.Width(300), GUILayout.Height(400));

        // カード名
        EditorGUILayout.LabelField("カード名", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(card.cardName, EditorStyles.largeLabel);

        EditorGUILayout.Space();

        // アイコン表示
        if (card.CardIcon != null)
        {
            EditorGUILayout.LabelField("アイコン", EditorStyles.boldLabel);
            Rect iconRect = GUILayoutUtility.GetRect(64, 64, GUILayout.Width(64), GUILayout.Height(64));
            GUI.DrawTexture(iconRect, card.CardIcon.texture);
        }

        EditorGUILayout.Space();

        // 攻撃属性
        EditorGUILayout.LabelField("攻撃属性", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(GetAttributeDisplayName(card.CardAttribute));

        EditorGUILayout.Space();

        // 説明文
        EditorGUILayout.LabelField("説明文", EditorStyles.boldLabel);
        string resolvedDesc = GetResolvedDescription(card);
        EditorGUILayout.TextArea(resolvedDesc, EditorStyles.wordWrappedLabel, GUILayout.Height(100));

        EditorGUILayout.Space();

        // 詳細情報
        EditorGUILayout.LabelField("詳細情報", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"ID: {card.cardId}");
        EditorGUILayout.LabelField($"タイプ: {GetCardTypeDisplayName(card.cardType)}");
        EditorGUILayout.LabelField($"威力: {card.basePower}");

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 見た目の表示
    /// </summary>
    private void OnGUI()
    {
        if (cardList == null)
        {
            LoadData();
        }

        // 上部のコントロール
        EditorGUILayout.BeginVertical("box");

        // リロードボタン
        if (GUILayout.Button("Reload Cards"))
        {
            LoadData();
        }

        EditorGUILayout.Space();

        // 表示オプション
        EditorGUILayout.BeginHorizontal();
        showResolvedDescription = EditorGUILayout.Toggle("説明文を表示", showResolvedDescription);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // 新規作成セクション
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("新規作成", GUILayout.Width(80)))
        {
            CreateNewCard();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // メイン表示エリア
        EditorGUILayout.BeginHorizontal();

        // 左側：テーブル表示
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

        // テーブル表示
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // ヘッダー行
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("ID", GUILayout.Width(30));
        GUILayout.Label("Name", GUILayout.Width(120));
        GUILayout.Label("Type", GUILayout.Width(80));
        GUILayout.Label("Attribute", GUILayout.Width(80));
        GUILayout.Label("Power", GUILayout.Width(60));
        GUILayout.Label("Icon", GUILayout.Width(60));
        GUILayout.Label(showResolvedDescription ? "Description (解決済み)" : "Description (生データ)", GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();

        // 削除予定のアイテムを記録するリスト
        List<CardEntity> toDelete = new List<CardEntity>();

        // 各行：CardEntity
        foreach (var card in cardList)
        {
            if (card == null) continue; // nullチェック

            EditorGUILayout.BeginHorizontal();

            // 変更開始検知
            EditorGUI.BeginChangeCheck();

            // 各フィールドの編集
            card.cardId = EditorGUILayout.IntField(card.cardId, GUILayout.Width(30));
            card.cardName = EditorGUILayout.TextField(card.cardName, GUILayout.Width(120));
            card.cardType = (CardEntity.CardType)EditorGUILayout.EnumPopup(card.cardType, GUILayout.Width(80));
            card.CardAttribute = (CardEntity.Attribute)EditorGUILayout.EnumPopup(card.CardAttribute, GUILayout.Width(80));
            card.basePower = EditorGUILayout.IntField(card.basePower, GUILayout.Width(60));
            card.CardIcon = (Sprite)EditorGUILayout.ObjectField(card.CardIcon, typeof(Sprite), false, GUILayout.Width(60));

            // 説明文の表示（編集は生データのみ、表示は選択に応じて切り替え）
            EditorGUILayout.BeginVertical(GUILayout.Width(400));

            // 編集用の生データフィールド
            card.CardDescription = EditorGUILayout.TextArea(card.CardDescription, GUILayout.Width(500), GUILayout.Height(40));

            // 表示用の解決済みフィールド（読み取り専用）
            if (showResolvedDescription)
            {
                EditorGUILayout.LabelField("表示用:", EditorStyles.miniLabel);
                string resolvedDesc = GetResolvedDescription(card);

                // 解決済み説明文を読み取り専用で表示
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextArea(resolvedDesc, GUILayout.Width(500), GUILayout.Height(40));
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.EndVertical();

            // 変更があったら保存処理
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(card, "Auto Save CardEntity");
                EditorUtility.SetDirty(card);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"CardEntity '{card.cardName}' を自動保存しました");
            }

            // アクションボタン
            EditorGUILayout.BeginHorizontal(GUILayout.Width(140));

            // プレビューボタン
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("プレビュー", GUILayout.Width(65)))
            {
                previewCard = card;
                showPreview = true;
            }
            GUI.backgroundColor = Color.white;

            // 選択ボタン
            if (GUILayout.Button("選択", GUILayout.Width(45)))
            {
                Selection.activeObject = card;
                EditorGUIUtility.PingObject(card);
            }

            // 削除ボタン
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("削除", GUILayout.Width(45)))
            {
                if (EditorUtility.DisplayDialog("削除確認",
                    $"'{card.cardName}' を削除しますか？\nこの操作は元に戻せません。",
                    "削除", "キャンセル"))
                {
                    toDelete.Add(card);
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();
        }

        // 削除処理
        foreach (var card in toDelete)
        {
            DeleteCard(card);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();

        // 右側：プレビュー表示
        if (showPreview && previewCard != null)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(320));

            // プレビューヘッダー
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("カードプレビュー", EditorStyles.boldLabel);
            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                showPreview = false;
                previewCard = null;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // プレビュー表示
            previewScrollPos = EditorGUILayout.BeginScrollView(previewScrollPos);
            DrawCardPreview(previewCard);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndHorizontal();

        // 統計情報
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"合計: {cardList.Count} 枚のカード", EditorStyles.miniLabel);

        // プレースホルダー使用例の表示
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("使用可能なプレースホルダー:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("  {Type} - カードタイプ（キャラ付き、武器付き、汎用）", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("  {Attribute} - 属性（斬、鈍、突、弾）", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("  {Power} - 基本威力", EditorStyles.miniLabel);

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// ScriptableObjectの作成
    /// </summary>
    private void CreateNewCard()
    {
        // 保存先フォルダが存在しない場合は作成
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
            AssetDatabase.Refresh();
        }

        // 新しいCardEntityを作成
        CardEntity newCard = ScriptableObject.CreateInstance<CardEntity>();

        // 初期値を設定
        newCard.cardId = GetNextAvailableId();
        newCard.cardName = "NotName";
        newCard.cardType = CardEntity.CardType.Universal;
        newCard.CardAttribute = CardEntity.Attribute.Slash;
        newCard.basePower = 0;
        newCard.CardDescription = "この{Type}カードは{Attribute}属性で、{Power}のダメージを与える。";

        // ファイル名を生成（重複を避ける）
        string fileName = $"{newCardName}_{newCard.cardId}.asset";
        string filePath = Path.Combine(savePath, fileName);

        // 既存のファイルと重複しないようにファイル名を調整
        int counter = 1;
        while (File.Exists(filePath))
        {
            fileName = $"{newCardName}_{newCard.cardId}_{counter}.asset";
            filePath = Path.Combine(savePath, fileName);
            counter++;
        }

        // アセットとして保存
        AssetDatabase.CreateAsset(newCard, filePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // リストを更新
        LoadData();

        // 作成したオブジェクトを選択
        Selection.activeObject = newCard;
        EditorGUIUtility.PingObject(newCard);

        Debug.Log($"新しいCardEntity '{newCard.cardName}' を作成しました: {filePath}");
    }

    /// <summary>
    /// ScriptableObjectの削除
    /// </summary>
    /// <param name="card"></param>
    private void DeleteCard(CardEntity card)
    {
        if (card == null) return;

        string assetPath = AssetDatabase.GetAssetPath(card);
        if (string.IsNullOrEmpty(assetPath)) return;

        // アセットを削除
        if (AssetDatabase.DeleteAsset(assetPath))
        {
            Debug.Log($"CardEntity '{card.cardName}' を削除しました");
            LoadData(); // リストを更新
        }
        else
        {
            Debug.LogError($"CardEntity '{card.cardName}' の削除に失敗しました");
        }
    }

    /// <summary>
    /// 次のID取得
    /// </summary>
    private int GetNextAvailableId()
    {
        if (cardList == null || cardList.Count == 0)
            return 1;

        // 既存のIDの最大値を取得して+1
        int maxId = cardList.Where(c => c != null).Max(c => c.cardId);
        return maxId + 1;
    }

    /// <summary>
    /// 変更したら保存
    /// </summary>
    private void SelectSavePath()
    {
        string selectedPath = EditorUtility.OpenFolderPanel("保存先フォルダを選択", "Assets", "");
        if (!string.IsNullOrEmpty(selectedPath))
        {
            // プロジェクトパスからの相対パスに変換
            string projectPath = Application.dataPath.Replace("/Assets", "");
            if (selectedPath.StartsWith(projectPath))
            {
                savePath = selectedPath.Replace(projectPath + "/", "");
            }
            else
            {
                Debug.LogWarning("プロジェクト外のフォルダは選択できません");
            }
        }
    }
}