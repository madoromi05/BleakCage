using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

/// <summary>
/// WeaponのGUIでのデータ管理ツール
/// </summary>
public class WeaponEntityTableEditor : EditorWindow
{
    private Vector2 scrollPos;
    private List<WeaponEntity> weaponList;
    private string newWeaponName = "Weapon";
    private string savePath = "Assets/Resources/WeaponEntityList";

    [MenuItem("Tools/Weapon Entity Table")]
    public static void OpenWindow()
    {
        GetWindow<WeaponEntityTableEditor>("Weapon Table");
    }

    private void OnEnable()
    {
        LoadData();
    }

    private void LoadData()
    {
        string[] guids = AssetDatabase.FindAssets("t:WeaponEntity", new string[] { savePath });
        weaponList = guids
            .Select(guid => AssetDatabase.LoadAssetAtPath<WeaponEntity>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(weapon => weapon != null)
            .ToList();
    }

    private void OnGUI()
    {
        if (weaponList == null)
        {
            LoadData();
        }

        EditorGUILayout.BeginVertical("box");
        if (GUILayout.Button("Reload Weapons"))
        {
            LoadData();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("新規作成", GUILayout.Width(80)))
        {
            CreateNewWeapon();
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("ID", GUILayout.Width(40));
        GUILayout.Label("Name", GUILayout.Width(120));
        GUILayout.Label("Power", GUILayout.Width(60));
        GUILayout.Label("Attribute", GUILayout.Width(80));
        GUILayout.Label("Peaky", GUILayout.Width(60));
        GUILayout.Label("Icon", GUILayout.Width(60));
        GUILayout.Label("Description", GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();

        List<WeaponEntity> toDelete = new List<WeaponEntity>();

        foreach (var weapon in weaponList)
        {
            if (weapon == null) continue;

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();

            weapon.ID = (uint)EditorGUILayout.IntField((int)weapon.ID, GUILayout.Width(40));
            weapon.Name = EditorGUILayout.TextField(weapon.Name, GUILayout.Width(120));
            weapon.AttackPower = EditorGUILayout.FloatField(weapon.AttackPower, GUILayout.Width(60));
            weapon.Attribute = (AttributeType)EditorGUILayout.EnumPopup(weapon.Attribute, GUILayout.Width(80));
            weapon.PeakyCoefficient = EditorGUILayout.FloatField(weapon.PeakyCoefficient, GUILayout.Width(60));
            weapon.Icon = (Sprite)EditorGUILayout.ObjectField(weapon.Icon, typeof(Sprite), false, GUILayout.Width(60));
            weapon.Description = EditorGUILayout.TextArea(weapon.Description, GUILayout.Width(200), GUILayout.Height(40));

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(weapon, "Auto Save WeaponEntity");
                EditorUtility.SetDirty(weapon);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"WeaponEntity '{weapon.Name}' を自動保存しました");
            }

            EditorGUILayout.BeginHorizontal(GUILayout.Width(100));
            if (GUILayout.Button("選択", GUILayout.Width(45)))
            {
                Selection.activeObject = weapon;
                EditorGUIUtility.PingObject(weapon);
            }

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("削除", GUILayout.Width(45)))
            {
                if (EditorUtility.DisplayDialog("削除確認",
                    $"'{weapon.Name}' を削除しますか？\nこの操作は元に戻せません。",
                    "削除", "キャンセル"))
                {
                    toDelete.Add(weapon);
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();
        }

        foreach (var weapon in toDelete)
        {
            DeleteWeapon(weapon);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"合計: {weaponList.Count} 件の武器", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
    }

    private void CreateNewWeapon()
    {
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
            AssetDatabase.Refresh();
        }

        WeaponEntity newWeapon = ScriptableObject.CreateInstance<WeaponEntity>();
        newWeapon.ID = (uint)GetNextAvailableId();
        newWeapon.Name = "New Weapon";
        newWeapon.AttackPower = 10f;
        newWeapon.Attribute = AttributeType.Bullet;
        newWeapon.PeakyCoefficient = 1.0f;
        newWeapon.Description = "新しい武器の説明文";

        string fileName = $"{newWeaponName}_{newWeapon.ID}.asset";
        string filePath = Path.Combine(savePath, fileName);
        int counter = 1;

        while (File.Exists(filePath))
        {
            fileName = $"{newWeaponName}_{newWeapon.ID}_{counter}.asset";
            filePath = Path.Combine(savePath, fileName);
            counter++;
        }

        AssetDatabase.CreateAsset(newWeapon, filePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        LoadData();
        Selection.activeObject = newWeapon;
        EditorGUIUtility.PingObject(newWeapon);

        Debug.Log($"新しいWeaponEntity '{newWeapon.Name}' を作成しました: {filePath}");
    }

    private void DeleteWeapon(WeaponEntity weapon)
    {
        if (weapon == null) return;

        string assetPath = AssetDatabase.GetAssetPath(weapon);
        if (string.IsNullOrEmpty(assetPath)) return;

        if (AssetDatabase.DeleteAsset(assetPath))
        {
            Debug.Log($"WeaponEntity '{weapon.Name}' を削除しました");
            LoadData();
        }
        else
        {
            Debug.LogError($"WeaponEntity '{weapon.Name}' の削除に失敗しました");
        }
    }

    private int GetNextAvailableId()
    {
        if (weaponList == null || weaponList.Count == 0)
            return 1;

        int maxId = weaponList.Where(w => w != null).Max(w => (int)w.ID);
        return maxId + 1;
    }
}
