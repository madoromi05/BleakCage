using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics.CodeAnalysis;

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
        // Assets/Resources/WeaponEntityList中のWeaponEntity をすべて検索
        string[] guids = AssetDatabase.FindAssets("t:WeaponEntity", new string[] { "Assets/Resources/WeaponEntityList" });
        weaponList = guids
            .Select(guid => AssetDatabase.LoadAssetAtPath<WeaponEntity>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(weapon => weapon != null) // nullチェック
            .ToList();
    }

    /// <summary>
    /// 見た目の表示
    /// </summary>
    private void OnGUI()
    {
        if (weaponList == null)
        {
            LoadData();
        }

        // 上部のコントロール
        EditorGUILayout.BeginVertical("box");

        // リロードボタン
        if (GUILayout.Button("Reload Weapons"))
        {
            LoadData();
        }

        EditorGUILayout.Space();

        // 新規作成セクション
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("新規作成", GUILayout.Width(80)))
        {
            CreateNewWeapon();
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
        GUILayout.Label("Attack", GUILayout.Width(60));
        GUILayout.Label("Attribute", GUILayout.Width(80));
        GUILayout.Label("Category", GUILayout.Width(100));
        GUILayout.Label("Peaky", GUILayout.Width(50));
        GUILayout.Label("Icon", GUILayout.Width(60));
        GUILayout.Label("Description", GUILayout.Width(150));
        EditorGUILayout.EndHorizontal();

        // 削除予定のアイテムを記録するリスト
        List<WeaponEntity> toDelete = new List<WeaponEntity>();

        // 各行：WeaponEntity
        foreach (var weapon in weaponList)
        {
            if (weapon == null) continue; // nullチェック

            EditorGUILayout.BeginHorizontal();

            // 変更開始検知
            EditorGUI.BeginChangeCheck();

            // 各フィールドの編集
            weapon.WeaponID = EditorGUILayout.IntField(weapon.WeaponID, GUILayout.Width(30));
            weapon.WeaponName = EditorGUILayout.TextField(weapon.WeaponName, GUILayout.Width(120));
            weapon.WeaponAttackPower = EditorGUILayout.IntField(weapon.WeaponAttackPower, GUILayout.Width(60));
            weapon.WeaponAttribute = (WeaponEntity.Attribute)EditorGUILayout.EnumPopup(weapon.WeaponAttribute, GUILayout.Width(80));
            weapon.weaponCategory = (WeaponEntity.WeaponCategory)EditorGUILayout.EnumPopup(weapon.weaponCategory, GUILayout.Width(100));
            weapon.PeakyCoefficient = EditorGUILayout.IntField(weapon.PeakyCoefficient, GUILayout.Width(50));
            weapon.WeaponIcon = (Sprite)EditorGUILayout.ObjectField(weapon.WeaponIcon, typeof(Sprite), false, GUILayout.Width(60));
            weapon.WeaponDescription = EditorGUILayout.TextField(weapon.WeaponDescription, GUILayout.Width(150));

            // 変更があったら保存処理
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(weapon, "Auto Save WeaponEntity");
                EditorUtility.SetDirty(weapon);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"WeaponEntity '{weapon.WeaponName}' を自動保存しました");
            }

            // アクションボタン
            EditorGUILayout.BeginHorizontal(GUILayout.Width(100));

            // 選択ボタン
            if (GUILayout.Button("選択", GUILayout.Width(45)))
            {
                Selection.activeObject = weapon;
                EditorGUIUtility.PingObject(weapon);
            }

            // 削除ボタン
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("削除", GUILayout.Width(45)))
            {
                if (EditorUtility.DisplayDialog("削除確認",
                    $"'{weapon.WeaponName}' を削除しますか？\nこの操作は元に戻せません。",
                    "削除", "キャンセル"))
                {
                    toDelete.Add(weapon);
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();
        }

        // 削除処理
        foreach (var weapon in toDelete)
        {
            DeleteWeapon(weapon);
        }

        EditorGUILayout.EndScrollView();

        // 統計情報
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"合計: {weaponList.Count} 個の武器", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// ScriptableObjectの作成
    /// </summary>
    private void CreateNewWeapon()
    {
        // 保存先フォルダが存在しない場合は作成
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
            AssetDatabase.Refresh();
        }

        // 新しいWeaponEntityを作成
        WeaponEntity newWeapon = ScriptableObject.CreateInstance<WeaponEntity>();

        // 初期値を設定
        newWeapon.WeaponID = GetNextAvailableId();
        newWeapon.WeaponName = "NotName";
        newWeapon.WeaponAttackPower = 0;
        newWeapon.WeaponAttribute = WeaponEntity.Attribute.Slash;
        newWeapon.weaponCategory = WeaponEntity.WeaponCategory.OneHandSword;
        newWeapon.PeakyCoefficient = 0;
        newWeapon.WeaponDescription = "";

        // ファイル名を生成（重複を避ける）
        string fileName = $"{newWeaponName}_{newWeapon.WeaponID}.asset";
        string filePath = Path.Combine(savePath, fileName);

        // 既存のファイルと重複しないようにファイル名を調整
        int counter = 1;
        while (File.Exists(filePath))
        {
            fileName = $"{newWeaponName}_{newWeapon.WeaponID}_{counter}.asset";
            filePath = Path.Combine(savePath, fileName);
            counter++;
        }

        // アセットとして保存
        AssetDatabase.CreateAsset(newWeapon, filePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // リストを更新
        LoadData();

        // 作成したオブジェクトを選択
        Selection.activeObject = newWeapon;
        EditorGUIUtility.PingObject(newWeapon);

        Debug.Log($"新しいWeaponEntity '{newWeapon.WeaponName}' を作成しました: {filePath}");
    }

    /// <summary>
    /// ScriptableObjectの削除
    /// </summary>
    /// <param name="weapon"></param>
    private void DeleteWeapon(WeaponEntity weapon)
    {
        if (weapon == null) return;

        string assetPath = AssetDatabase.GetAssetPath(weapon);
        if (string.IsNullOrEmpty(assetPath)) return;

        // アセットを削除
        if (AssetDatabase.DeleteAsset(assetPath))
        {
            Debug.Log($"WeaponEntity '{weapon.WeaponName}' を削除しました");
            LoadData(); // リストを更新
        }
        else
        {
            Debug.LogError($"WeaponEntity '{weapon.WeaponName}' の削除に失敗しました");
        }
    }

    /// <summary>
    /// 次のID取得
    /// </summary>
    private int GetNextAvailableId()
    {
        if (weaponList == null || weaponList.Count == 0)
            return 1;

        // 既存のIDの最大値を取得して+1
        int maxId = weaponList.Where(w => w != null).Max(w => w.WeaponID);
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