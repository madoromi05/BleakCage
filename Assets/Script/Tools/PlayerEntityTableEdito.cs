using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// PlayerのGUIでのデータ管理ツール
/// </summary>
public class PlayerEntityTableEditor : EditorWindow
{
    private Vector2 scrollPos;
    private List<PlayerEntity> playerList;
    private string newPlayerName = "Player";
    private string savePath = "Assets/Resources/PlayerEntityList";
    private bool showResolvedDescription = true; // 解決済み説明文を表示するかどうか

    // プレビュー用の変数
    private PlayerEntity previewPlayer;
    private bool showPreview = false;
    private Vector2 previewScrollPos;

    [MenuItem("Tools/Player Entity Table")]
    public static void OpenWindow()
    {
        GetWindow<PlayerEntityTableEditor>("Player Table");
    }

    private void OnEnable()
    {
        LoadData();
    }

    private void LoadData()
    {
        // Assets/Resources/PlayerEntityList中のPlayerEntity をすべて検索
        string[] guids = AssetDatabase.FindAssets("t:PlayerEntity", new string[] { "Assets/Resources/PlayerEntityList" });
        playerList = guids
            .Select(guid => AssetDatabase.LoadAssetAtPath<PlayerEntity>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(player => player != null) // nullチェック
            .ToList();
    }

    /// <summary>
    /// 属性の日本語表示名を取得
    /// </summary>
    private string GetAttributeDisplayName(PlayerEntity.Attribute attribute)
    {
        switch (attribute)
        {
            case PlayerEntity.Attribute.Slash:
                return "斬";
            case PlayerEntity.Attribute.Blunt:
                return "鈍";
            case PlayerEntity.Attribute.Pierce:
                return "突";
            case PlayerEntity.Attribute.Bullet:
                return "弾";
            default:
                return attribute.ToString();
        }
    }

    /// <summary>
    /// 説明文のプレースホルダーを実際の値に置換
    /// </summary>
    private string GetResolvedDescription(PlayerEntity player)
    {
        if (string.IsNullOrEmpty(player.PlayerDescription))
            return "";

        return player.PlayerDescription
            .Replace("{Name}", player.PlayerName)
            .Replace("{HP}", player.PlayerHP.ToString())
            .Replace("{Attack}", player.PlayerAttackPower.ToString())
            .Replace("{Defense}", player.PlayerDefensePower.ToString())
            .Replace("{Attribute}", GetAttributeDisplayName(player.PlayerAttribute));
    }

    /// <summary>
    /// プレイヤープレビューの表示
    /// </summary>
    private void DrawPlayerPreview(PlayerEntity player)
    {
        if (player == null) return;

        EditorGUILayout.BeginVertical("box", GUILayout.Width(300), GUILayout.Height(400));

        // プレイヤー名
        EditorGUILayout.LabelField("プレイヤー名", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(player.PlayerName, EditorStyles.largeLabel);

        EditorGUILayout.Space();

        // アイコン表示
        if (player.PlayerIcon != null)
        {
            EditorGUILayout.LabelField("アイコン", EditorStyles.boldLabel);
            Rect iconRect = GUILayoutUtility.GetRect(64, 64, GUILayout.Width(64), GUILayout.Height(64));
            GUI.DrawTexture(iconRect, player.PlayerIcon.texture);
        }

        // SDアイコン表示
        if (player.PlayerSDIcon != null)
        {
            EditorGUILayout.LabelField("SDアイコン", EditorStyles.boldLabel);
            Rect sdIconRect = GUILayoutUtility.GetRect(64, 64, GUILayout.Width(64), GUILayout.Height(64));
            GUI.DrawTexture(sdIconRect, player.PlayerSDIcon.texture);
        }

        EditorGUILayout.Space();

        // 属性
        EditorGUILayout.LabelField("属性", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(GetAttributeDisplayName(player.PlayerAttribute));

        EditorGUILayout.Space();

        // 説明文
        EditorGUILayout.LabelField("説明文", EditorStyles.boldLabel);
        string resolvedDesc = GetResolvedDescription(player);
        EditorGUILayout.TextArea(resolvedDesc, EditorStyles.wordWrappedLabel, GUILayout.Height(100));

        EditorGUILayout.Space();

        // 詳細情報
        EditorGUILayout.LabelField("詳細情報", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"ID: {player.PlayerId}");
        EditorGUILayout.LabelField($"HP: {player.PlayerHP}");
        EditorGUILayout.LabelField($"攻撃力: {player.PlayerAttackPower}");
        EditorGUILayout.LabelField($"防御力: {player.PlayerDefensePower}");

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 見た目の表示
    /// </summary>
    private void OnGUI()
    {
        if (playerList == null)
        {
            LoadData();
        }

        // 上部のコントロール
        EditorGUILayout.BeginVertical("box");

        // リロードボタン
        if (GUILayout.Button("Reload Players"))
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
            CreateNewPlayer();
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
        GUILayout.Label("SD Icon", GUILayout.Width(60));
        GUILayout.Label(showResolvedDescription ? "Description (解決済み)" : "Description (生データ)", GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();

        // 削除予定のアイテムを記録するリスト
        List<PlayerEntity> toDelete = new List<PlayerEntity>();

        // 各行：PlayerEntity
        foreach (var player in playerList)
        {
            if (player == null) continue; // nullチェック

            EditorGUILayout.BeginHorizontal();

            // 変更開始検知
            EditorGUI.BeginChangeCheck();

            // 各フィールドの編集
            player.PlayerId = EditorGUILayout.IntField(player.PlayerId, GUILayout.Width(30));
            player.PlayerName = EditorGUILayout.TextField(player.PlayerName, GUILayout.Width(120));
            player.PlayerHP = EditorGUILayout.IntField(player.PlayerHP, GUILayout.Width(60));
            player.PlayerAttackPower = EditorGUILayout.IntField(player.PlayerAttackPower, GUILayout.Width(60));
            player.PlayerDefensePower = EditorGUILayout.IntField(player.PlayerDefensePower, GUILayout.Width(60));
            player.PlayerAttribute = (PlayerEntity.Attribute)EditorGUILayout.EnumPopup(player.PlayerAttribute, GUILayout.Width(80));
            player.PlayerIcon = (Sprite)EditorGUILayout.ObjectField(player.PlayerIcon, typeof(Sprite), false, GUILayout.Width(60));
            player.PlayerSDIcon = (Sprite)EditorGUILayout.ObjectField(player.PlayerSDIcon, typeof(Sprite), false, GUILayout.Width(60));

            // 説明文の表示（編集は生データのみ、表示は選択に応じて切り替え）
            EditorGUILayout.BeginVertical(GUILayout.Width(400));

            // 編集用の生データフィールド
            player.PlayerDescription = EditorGUILayout.TextArea(player.PlayerDescription, GUILayout.Width(500), GUILayout.Height(40));

            // 表示用の解決済みフィールド（読み取り専用）
            if (showResolvedDescription)
            {
                EditorGUILayout.LabelField("表示用:", EditorStyles.miniLabel);
                string resolvedDesc = GetResolvedDescription(player);

                // 解決済み説明文を読み取り専用で表示
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextArea(resolvedDesc, GUILayout.Width(500), GUILayout.Height(40));
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.EndVertical();

            // 変更があったら保存処理
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(player, "Auto Save PlayerEntity");
                EditorUtility.SetDirty(player);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"PlayerEntity '{player.PlayerName}' を自動保存しました");
            }

            // アクションボタン
            EditorGUILayout.BeginHorizontal(GUILayout.Width(140));

            // プレビューボタン
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("プレビュー", GUILayout.Width(65)))
            {
                previewPlayer = player;
                showPreview = true;
            }
            GUI.backgroundColor = Color.white;

            // 選択ボタン
            if (GUILayout.Button("選択", GUILayout.Width(45)))
            {
                Selection.activeObject = player;
                EditorGUIUtility.PingObject(player);
            }

            // 削除ボタン
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("削除", GUILayout.Width(45)))
            {
                if (EditorUtility.DisplayDialog("削除確認",
                    $"'{player.PlayerName}' を削除しますか？\nこの操作は元に戻せません。",
                    "削除", "キャンセル"))
                {
                    toDelete.Add(player);
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();
        }

        // 削除処理
        foreach (var player in toDelete)
        {
            DeletePlayer(player);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();

        // 右側：プレビュー表示
        if (showPreview && previewPlayer != null)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(320));

            // プレビューヘッダー
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("プレイヤープレビュー", EditorStyles.boldLabel);
            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                showPreview = false;
                previewPlayer = null;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // プレビュー表示
            previewScrollPos = EditorGUILayout.BeginScrollView(previewScrollPos);
            DrawPlayerPreview(previewPlayer);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndHorizontal();

        // 統計情報
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"合計: {playerList.Count} 人のプレイヤー", EditorStyles.miniLabel);

        // プレースホルダー使用例の表示
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("使用可能なプレースホルダー:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("  {Name} - プレイヤーの名前", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("  {HP} - HP", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("  {Attack} - 攻撃力", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("  {Defense} - 防御力", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("  {Attribute} - 属性（斬、鈍、突、弾）", EditorStyles.miniLabel);

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// ScriptableObjectの作成
    /// </summary>
    private void CreateNewPlayer()
    {
        // 保存先フォルダが存在しない場合は作成
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
            AssetDatabase.Refresh();
        }

        // 新しいPlayerEntityを作成
        PlayerEntity newPlayer = ScriptableObject.CreateInstance<PlayerEntity>();

        // 初期値を設定
        newPlayer.PlayerId = GetNextAvailableId();
        newPlayer.PlayerName = "NotName";
        newPlayer.PlayerHP = 0;
        newPlayer.PlayerAttackPower = 0;
        newPlayer.PlayerDefensePower = 0;
        newPlayer.PlayerAttribute = PlayerEntity.Attribute.Slash;
        newPlayer.PlayerDescription = "{Name}は{Attribute}属性のプレイヤーで、HP{HP}、攻撃力{Attack}、防御力{Defense}を持つ。";

        // ファイル名を生成（重複を避ける）
        string fileName = $"{newPlayerName}_{newPlayer.PlayerId}.asset";
        string filePath = Path.Combine(savePath, fileName);

        // 既存のファイルと重複しないようにファイル名を調整
        int counter = 1;
        while (File.Exists(filePath))
        {
            fileName = $"{newPlayerName}_{newPlayer.PlayerId}_{counter}.asset";
            filePath = Path.Combine(savePath, fileName);
            counter++;
        }

        // アセットとして保存
        AssetDatabase.CreateAsset(newPlayer, filePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // リストを更新
        LoadData();

        // 作成したオブジェクトを選択
        Selection.activeObject = newPlayer;
        EditorGUIUtility.PingObject(newPlayer);

        Debug.Log($"新しいPlayerEntity '{newPlayer.PlayerName}' を作成しました: {filePath}");
    }

    /// <summary>
    /// ScriptableObjectの削除
    /// </summary>
    /// <param name="player"></param>
    private void DeletePlayer(PlayerEntity player)
    {
        if (player == null) return;

        string assetPath = AssetDatabase.GetAssetPath(player);
        if (string.IsNullOrEmpty(assetPath)) return;

        // アセットを削除
        if (AssetDatabase.DeleteAsset(assetPath))
        {
            Debug.Log($"PlayerEntity '{player.PlayerName}' を削除しました");
            LoadData(); // リストを更新
        }
        else
        {
            Debug.LogError($"PlayerEntity '{player.PlayerName}' の削除に失敗しました");
        }
    }

    /// <summary>
    /// 次のID取得
    /// </summary>
    private int GetNextAvailableId()
    {
        if (playerList == null || playerList.Count == 0)
            return 1;

        // 既存のIDの最大値を取得して+1
        int maxId = playerList.Where(p => p != null).Max(p => p.PlayerId);
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