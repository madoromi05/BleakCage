using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

/// <summary>
/// EnemyのGUIでのデータ管理ツール
/// </summary>
public class EnemyEntityTableEditor : EditorWindow
{
    private Vector2 scrollPos;
    private List<EnemyEntity> enemyList;
    private string newEnemyName = "Enemy";
    private string savePath = "Assets/Resources/EnemyEntityList";

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
        string[] guids = AssetDatabase.FindAssets("t:EnemyEntity", new string[] { savePath });
        enemyList = guids
            .Select(guid => AssetDatabase.LoadAssetAtPath<EnemyEntity>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(enemy => enemy != null)
            .ToList();
    }

    private void OnGUI()
    {
        if (enemyList == null)
        {
            LoadData();
        }

        EditorGUILayout.BeginVertical("box");

        if (GUILayout.Button("Reload Enemies"))
        {
            LoadData();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("新規作成", GUILayout.Width(80)))
        {
            CreateNewEnemy();
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // ヘッダー
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("ID", GUILayout.Width(30));
        GUILayout.Label("Name", GUILayout.Width(120));
        GUILayout.Label("HP", GUILayout.Width(80));
        GUILayout.Label("ATK", GUILayout.Width(80));
        GUILayout.Label("DEF", GUILayout.Width(80));
        GUILayout.Label("Attribute", GUILayout.Width(80));
        GUILayout.Label("Icon", GUILayout.Width(60));
        GUILayout.Label("Description", GUILayout.Width(300));
        EditorGUILayout.EndHorizontal();

        List<EnemyEntity> toDelete = new List<EnemyEntity>();

        foreach (var enemy in enemyList)
        {
            if (enemy == null) continue;

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();

            enemy.EnemyID = EditorGUILayout.IntField(enemy.EnemyID, GUILayout.Width(30));
            enemy.EnemyName = EditorGUILayout.TextField(enemy.EnemyName, GUILayout.Width(120));
            enemy.EnemyHP = EditorGUILayout.FloatField(enemy.EnemyHP, GUILayout.Width(80));
            enemy.EnemyAttackPower = EditorGUILayout.FloatField(enemy.EnemyAttackPower, GUILayout.Width(80));
            enemy.EnemyDefensePower = EditorGUILayout.FloatField(enemy.EnemyDefensePower, GUILayout.Width(80));
            enemy.EnemyAttribute = (AttributeType)EditorGUILayout.EnumPopup(enemy.EnemyAttribute, GUILayout.Width(80));
            enemy.EnemySprite = (Sprite)EditorGUILayout.ObjectField(enemy.EnemySprite, typeof(Sprite), false, GUILayout.Width(60));
            enemy.EnemyDescription = EditorGUILayout.TextArea(enemy.EnemyDescription, GUILayout.Width(500), GUILayout.Height(40));

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(enemy, "Auto Save EnemyEntity");
                EditorUtility.SetDirty(enemy);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"EnemyEntity '{enemy.EnemyName}' を自動保存しました");
            }

            EditorGUILayout.BeginHorizontal(GUILayout.Width(100));
            if (GUILayout.Button("選択", GUILayout.Width(45)))
            {
                Selection.activeObject = enemy;
                EditorGUIUtility.PingObject(enemy);
            }

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

        foreach (var enemy in toDelete)
        {
            DeleteEnemy(enemy);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"合計: {enemyList.Count} 体の敵", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
    }

    private void CreateNewEnemy()
    {
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
            AssetDatabase.Refresh();
        }

        EnemyEntity newEnemy = ScriptableObject.CreateInstance<EnemyEntity>();

        newEnemy.EnemyID = GetNextAvailableId();
        newEnemy.EnemyName = "New Enemy";
        newEnemy.EnemyHP = 100f;
        newEnemy.EnemyAttackPower = 10f;
        newEnemy.EnemyDefensePower = 5f;
        newEnemy.EnemyAttribute = AttributeType.Bullet;
        newEnemy.EnemyDescription = "新しい敵の説明文";

        string fileName = $"{newEnemyName}_{newEnemy.EnemyID}.asset";
        string filePath = Path.Combine(savePath, fileName);
        int counter = 1;
        while (File.Exists(filePath))
        {
            fileName = $"{newEnemyName}_{newEnemy.EnemyID}_{counter}.asset";
            filePath = Path.Combine(savePath, fileName);
            counter++;
        }

        AssetDatabase.CreateAsset(newEnemy, filePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        LoadData();
        Selection.activeObject = newEnemy;
        EditorGUIUtility.PingObject(newEnemy);

        Debug.Log($"新しいEnemyEntity '{newEnemy.EnemyName}' を作成しました: {filePath}");
    }

    private void DeleteEnemy(EnemyEntity enemy)
    {
        if (enemy == null) return;

        string assetPath = AssetDatabase.GetAssetPath(enemy);
        if (string.IsNullOrEmpty(assetPath)) return;

        if (AssetDatabase.DeleteAsset(assetPath))
        {
            Debug.Log($"EnemyEntity '{enemy.EnemyName}' を削除しました");
            LoadData();
        }
        else
        {
            Debug.LogError($"EnemyEntity '{enemy.EnemyName}' の削除に失敗しました");
        }
    }

    private int GetNextAvailableId()
    {
        if (enemyList == null || enemyList.Count == 0)
            return 1;

        int maxId = enemyList.Where(e => e != null).Max(e => e.EnemyID);
        return maxId + 1;
    }
}
