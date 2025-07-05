using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// EnemyのGUIでのデータ管理ツール
/// </summary>
public class EnemyEntityTableEditor : EditorWindow
{
    private Vector2 scrollPos;
    private List<EnemyEntity> enemyList;
    private string newEnemyName = "Enemy";
    private string savePath = "Assets/Resources/EnemyEntityList";
    private bool showResolvedDescription = true; // 解決済み説明文を表示するかどうか

    // プレビュー用の変数
    private EnemyEntity previewEnemy;
    private bool showPreview = false;
    private Vector2 previewScrollPos;

    [MenuItem("Tools/Enemy Entity Table")]
    public static void OpenWindow()
    {
        GetWindow<EnemyEntityTableEditor>("Enemy Table");
    }

    private void OnEnable()
    {
        LoadData();
    }

    private void LoadData()
    {
        // Assets/Resources/EnemyEntityList中のEnemyEntity をすべて検索
        string[] guids = AssetDatabase.FindAssets("t:EnemyEntity", new string[] { "Assets/Resources/EnemyEntityList" });
        enemyList = guids
            .Select(guid => AssetDatabase.LoadAssetAtPath<EnemyEntity>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(enemy => enemy != null) // nullチェック
            .ToList();
    }

    /// <summary>
    /// 属性の日本語表示名を取得
    /// </summary>
    private string GetAttributeDisplayName(EnemyEntity.Attribute attribute)
    {
        switch (attribute)
        {
            case EnemyEntity.Attribute.Slash:
                return "斬";
            case EnemyEntity.Attribute.Blunt:
                return "鈍";
            case EnemyEntity.Attribute.Pierce:
                return "突";
            case EnemyEntity.Attribute.Bullet:
                return "弾";
            default:
                return attribute.ToString();
        }
    }

    /// <summary>
    /// 説明文のプレースホルダーを実際の値に置換
    /// </summary>
    private string GetResolvedDescription(EnemyEntity enemy)
    {
        if (string.IsNullOrEmpty(enemy.EnemyDescription))
            return "";

        return enemy.EnemyDescription
            .Replace("{Name}", enemy.EnemyName)
            .Replace("{HP}", enemy.EnemyHP.ToString())
            .Replace("{Attack}", enemy.EnemyAttackPower.ToString())
            .Replace("{Defense}", enemy.EnemyDefensePower.ToString())
            .Replace("{Attribute}", GetAttributeDisplayName(enemy.EnemyAttribute));
    }

    /// <summary>
    /// 敵プレビューの表示
    /// </summary>
    private void DrawEnemyPreview(EnemyEntity enemy)
    {
        if (enemy == null) return;

        EditorGUILayout.BeginVertical("box", GUILayout.Width(300), GUILayout.Height(400));

        // 敵名
        EditorGUILayout.LabelField("敵名", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(enemy.EnemyName, EditorStyles.largeLabel);

        EditorGUILayout.Space();

        // アイコン表示
        if (enemy.EnemyIcon != null)
        {
            EditorGUILayout.LabelField("アイコン", EditorStyles.boldLabel);
            Rect iconRect = GUILayoutUtility.GetRect(64, 64, GUILayout.Width(64), GUILayout.Height(64));
            GUI.DrawTexture(iconRect, enemy.EnemyIcon.texture);
        }

        EditorGUILayout.Space();

        // 属性
        EditorGUILayout.LabelField("属性", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(GetAttributeDisplayName(enemy.EnemyAttribute));

        EditorGUILayout.Space();

        // 説明文
        EditorGUILayout.LabelField("説明文", EditorStyles.boldLabel);
        string resolvedDesc = GetResolvedDescription(enemy);
        EditorGUILayout.TextArea(resolvedDesc, EditorStyles.wordWrappedLabel, GUILayout.Height(100));

        EditorGUILayout.Space();

        // 詳細情報
        EditorGUILayout.LabelField("詳細情報", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"ID: {enemy.EnemyId}");
        EditorGUILayout.LabelField($"HP: {enemy.EnemyHP}");
        EditorGUILayout.LabelField($"攻撃力: {enemy.EnemyAttackPower}");
        EditorGUILayout.LabelField($"防御力: {enemy.EnemyDefensePower}");

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 見た目の表示
    /// </summary>
    private void OnGUI()
    {
        if (enemyList == null)
        {
            LoadData();
        }

        // 上部のコントロール
        EditorGUILayout.BeginVertical("box");

        // リロードボタン
        if (GUILayout.Button("Reload Enemies"))
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
            CreateNewEnemy();
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
        GUILayout.Label("HP", GUILayout.Width(60));
        GUILayout.Label("Attack", GUILayout.Width(60));
        GUILayout.Label("Defense", GUILayout.Width(60));
        GUILayout.Label("Attribute", GUILayout.Width(80));
        GUILayout.Label("Icon", GUILayout.Width(60));
        GUILayout.Label(showResolvedDescription ? "Description (解決済み)" : "Description (生データ)", GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();

        // 削除予定のアイテムを記録するリスト
        List<EnemyEntity> toDelete = new List<EnemyEntity>();

        // 各行：EnemyEntity
        foreach (var enemy in enemyList)
        {
            if (enemy == null) continue; // nullチェック

            EditorGUILayout.BeginHorizontal();

            // 変更開始検知
            EditorGUI.BeginChangeCheck();

            // 各フィールドの編集
            enemy.EnemyId = EditorGUILayout.IntField(enemy.EnemyId, GUILayout.Width(30));
            enemy.EnemyName = EditorGUILayout.TextField(enemy.EnemyName, GUILayout.Width(120));
            enemy.EnemyHP = EditorGUILayout.FloatField(enemy.EnemyHP, GUILayout.Width(60));
            enemy.EnemyAttackPower = EditorGUILayout.FloatField(enemy.EnemyAttackPower, GUILayout.Width(60));
            enemy.EnemyDefensePower = EditorGUILayout.FloatField(enemy.EnemyDefensePower, GUILayout.Width(60));
            enemy.EnemyAttribute = (EnemyEntity.Attribute)EditorGUILayout.EnumPopup(enemy.EnemyAttribute, GUILayout.Width(80));
            enemy.EnemyIcon = (Sprite)EditorGUILayout.ObjectField(enemy.EnemyIcon, typeof(Sprite), false, GUILayout.Width(60));

            // 説明文の表示（編集は生データのみ、表示は選択に応じて切り替え）
            EditorGUILayout.BeginVertical(GUILayout.Width(400));

            // 編集用の生データフィールド
            enemy.EnemyDescription = EditorGUILayout.TextArea(enemy.EnemyDescription, GUILayout.Width(500), GUILayout.Height(40));

            // 表示用の解決済みフィールド（読み取り専用）
            if (showResolvedDescription)
            {
                EditorGUILayout.LabelField("表示用:", EditorStyles.miniLabel);
                string resolvedDesc = GetResolvedDescription(enemy);

                // 解決済み説明文を読み取り専用で表示
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextArea(resolvedDesc, GUILayout.Width(500), GUILayout.Height(40));
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.EndVertical();

            // 変更があったら保存処理
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(enemy, "Auto Save EnemyEntity");
                EditorUtility.SetDirty(enemy);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"EnemyEntity '{enemy.EnemyName}' を自動保存しました");
            }

            // アクションボタン
            EditorGUILayout.BeginHorizontal(GUILayout.Width(140));

            // プレビューボタン
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("プレビュー", GUILayout.Width(65)))
            {
                previewEnemy = enemy;
                showPreview = true;
            }
            GUI.backgroundColor = Color.white;

            // 選択ボタン
            if (GUILayout.Button("選択", GUILayout.Width(45)))
            {
                Selection.activeObject = enemy;
                EditorGUIUtility.PingObject(enemy);
            }

            // 削除ボタン
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("削除", GUILayout.Width(45)))
            {
                if (EditorUtility.DisplayDialog("削除確認",
                    $"'{enemy.EnemyName}' を削除しますか？\nこの操作は元に戻せません。",
                    "削除", "キャンセル"))
                {
                    toDelete.Add(enemy);
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();
        }

        // 削除処理
        foreach (var enemy in toDelete)
        {
            DeleteEnemy(enemy);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();

        // 右側：プレビュー表示
        if (showPreview && previewEnemy != null)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(320));

            // プレビューヘッダー
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("敵プレビュー", EditorStyles.boldLabel);
            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                showPreview = false;
                previewEnemy = null;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // プレビュー表示
            previewScrollPos = EditorGUILayout.BeginScrollView(previewScrollPos);
            DrawEnemyPreview(previewEnemy);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndHorizontal();

        // 統計情報
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"合計: {enemyList.Count} 体の敵", EditorStyles.miniLabel);

        // プレースホルダー使用例の表示
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("使用可能なプレースホルダー:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("  {Name} - 敵の名前", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("  {HP} - HP", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("  {Attack} - 攻撃力", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("  {Defense} - 防御力", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("  {Attribute} - 属性（斬、鈍、突、弾）", EditorStyles.miniLabel);

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// ScriptableObjectの作成
    /// </summary>
    private void CreateNewEnemy()
    {
        // 保存先フォルダが存在しない場合は作成
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
            AssetDatabase.Refresh();
        }

        // 新しいEnemyEntityを作成
        EnemyEntity newEnemy = ScriptableObject.CreateInstance<EnemyEntity>();

        // 初期値を設定
        newEnemy.EnemyId = GetNextAvailableId();
        newEnemy.EnemyName = "NotName";
        newEnemy.EnemyHP = 0f;
        newEnemy.EnemyAttackPower = 0;
        newEnemy.EnemyDefensePower = 0;
        newEnemy.EnemyAttribute = EnemyEntity.Attribute.Slash;
        newEnemy.EnemyDescription = "{Name}は{Attribute}属性の敵で、HP{HP}、攻撃力{Attack}、防御力{Defense}を持つ。";

        // ファイル名を生成（重複を避ける）
        string fileName = $"{newEnemyName}_{newEnemy.EnemyId}.asset";
        string filePath = Path.Combine(savePath, fileName);

        // 既存のファイルと重複しないようにファイル名を調整
        int counter = 1;
        while (File.Exists(filePath))
        {
            fileName = $"{newEnemyName}_{newEnemy.EnemyId}_{counter}.asset";
            filePath = Path.Combine(savePath, fileName);
            counter++;
        }

        // アセットとして保存
        AssetDatabase.CreateAsset(newEnemy, filePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // リストを更新
        LoadData();

        // 作成したオブジェクトを選択
        Selection.activeObject = newEnemy;
        EditorGUIUtility.PingObject(newEnemy);

        Debug.Log($"新しいEnemyEntity '{newEnemy.EnemyName}' を作成しました: {filePath}");
    }

    /// <summary>
    /// ScriptableObjectの削除
    /// </summary>
    /// <param name="enemy"></param>
    private void DeleteEnemy(EnemyEntity enemy)
    {
        if (enemy == null) return;

        string assetPath = AssetDatabase.GetAssetPath(enemy);
        if (string.IsNullOrEmpty(assetPath)) return;

        // アセットを削除
        if (AssetDatabase.DeleteAsset(assetPath))
        {
            Debug.Log($"EnemyEntity '{enemy.EnemyName}' を削除しました");
            LoadData(); // リストを更新
        }
        else
        {
            Debug.LogError($"EnemyEntity '{enemy.EnemyName}' の削除に失敗しました");
        }
    }

    /// <summary>
    /// 次のID取得
    /// </summary>
    private int GetNextAvailableId()
    {
        if (enemyList == null || enemyList.Count == 0)
            return 1;

        // 既存のIDの最大値を取得して+1
        int maxId = enemyList.Where(e => e != null).Max(e => e.EnemyId);
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