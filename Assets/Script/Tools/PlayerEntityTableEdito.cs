using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

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
        string[] guids = AssetDatabase.FindAssets("t:PlayerEntity", new string[] { savePath });
        playerList = guids
            .Select(guid => AssetDatabase.LoadAssetAtPath<PlayerEntity>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(player => player != null)
            .ToList();
    }

    private void OnGUI()
    {
        if (playerList == null)
        {
            LoadData();
        }

        EditorGUILayout.BeginVertical("box");

        if (GUILayout.Button("Reload Players"))
        {
            LoadData();
        }

        if (GUILayout.Button("新規作成", GUILayout.Width(100)))
        {
            CreateNewPlayer();
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // ヘッダー
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("ID", GUILayout.Width(30));
        GUILayout.Label("Name", GUILayout.Width(120));
        GUILayout.Label("Level", GUILayout.Width(50));
        GUILayout.Label("HP", GUILayout.Width(60));
        GUILayout.Label("ATK", GUILayout.Width(60));
        GUILayout.Label("DEF", GUILayout.Width(60));
        GUILayout.Label("Attr", GUILayout.Width(80));
        GUILayout.Label("Icon", GUILayout.Width(60));
        GUILayout.Label("SD", GUILayout.Width(60));
        GUILayout.Label("Description", GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();

        List<PlayerEntity> toDelete = new List<PlayerEntity>();

        foreach (var player in playerList)
        {
            if (player == null) continue;

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();

            player.PlayerId = EditorGUILayout.IntField(player.PlayerId, GUILayout.Width(30));
            player.PlayerName = EditorGUILayout.TextField(player.PlayerName, GUILayout.Width(120));
            player.PlayerLevel = EditorGUILayout.IntField(player.PlayerLevel, GUILayout.Width(50));
            player.PlayerHP = EditorGUILayout.FloatField(player.PlayerHP, GUILayout.Width(60));
            player.PlayerAttackPower = EditorGUILayout.FloatField(player.PlayerAttackPower, GUILayout.Width(60));
            player.PlayerDefensePower = EditorGUILayout.FloatField(player.PlayerDefensePower, GUILayout.Width(60));
            player.PlayerAttribute = (AttributeType)EditorGUILayout.EnumPopup(player.PlayerAttribute, GUILayout.Width(80));
            player.PlayerIcon = (Sprite)EditorGUILayout.ObjectField(player.PlayerIcon, typeof(Sprite), false, GUILayout.Width(60));
            player.PlayerSDIcon = (Sprite)EditorGUILayout.ObjectField(player.PlayerSDIcon, typeof(Sprite), false, GUILayout.Width(60));
            player.PlayerDescription = EditorGUILayout.TextArea(player.PlayerDescription, GUILayout.Width(500), GUILayout.Height(40));

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(player, "Auto Save PlayerEntity");
                EditorUtility.SetDirty(player);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"PlayerEntity '{player.PlayerName}' を自動保存しました");
            }

            EditorGUILayout.BeginHorizontal(GUILayout.Width(100));

            if (GUILayout.Button("選択", GUILayout.Width(45)))
            {
                Selection.activeObject = player;
                EditorGUIUtility.PingObject(player);
            }

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

        foreach (var player in toDelete)
        {
            DeletePlayer(player);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"合計: {playerList.Count} 人のプレイヤー", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
    }

    private void CreateNewPlayer()
    {
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
            AssetDatabase.Refresh();
        }

        PlayerEntity newPlayer = ScriptableObject.CreateInstance<PlayerEntity>();

        newPlayer.PlayerId = GetNextAvailableId();
        newPlayer.PlayerName = "New Player";
        newPlayer.PlayerLevel = 1;
        newPlayer.PlayerHP = 100f;
        newPlayer.PlayerAttackPower = 10f;
        newPlayer.PlayerDefensePower = 5f;
        newPlayer.PlayerAttribute = AttributeType.Slash;
        newPlayer.PlayerDescription = "新しいプレイヤーの説明文";

        string fileName = $"{newPlayerName}_{newPlayer.PlayerId}.asset";
        string filePath = Path.Combine(savePath, fileName);
        int counter = 1;

        while (File.Exists(filePath))
        {
            fileName = $"{newPlayerName}_{newPlayer.PlayerId}_{counter}.asset";
            filePath = Path.Combine(savePath, fileName);
            counter++;
        }

        AssetDatabase.CreateAsset(newPlayer, filePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        LoadData();
        Selection.activeObject = newPlayer;
        EditorGUIUtility.PingObject(newPlayer);

        Debug.Log($"新しいPlayerEntity '{newPlayer.PlayerName}' を作成しました: {filePath}");
    }

    private void DeletePlayer(PlayerEntity player)
    {
        if (player == null) return;

        string assetPath = AssetDatabase.GetAssetPath(player);
        if (string.IsNullOrEmpty(assetPath)) return;

        if (AssetDatabase.DeleteAsset(assetPath))
        {
            Debug.Log($"PlayerEntity '{player.PlayerName}' を削除しました");
            LoadData();
        }
        else
        {
            Debug.LogError($"PlayerEntity '{player.PlayerName}' の削除に失敗しました");
        }
    }

    private int GetNextAvailableId()
    {
        if (playerList == null || playerList.Count == 0)
            return 1;

        int maxId = playerList.Where(p => p != null).Max(p => p.PlayerId);
        return maxId + 1;
    }
}
