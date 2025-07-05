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
            EditorGUILayout.BeginHorizontal(GUILayout.Width(100));

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

        // 統計情報
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"合計: {playerList.Count} 体のプレイヤー", EditorStyles.miniLabel);
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