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
    private bool showResolvedDescription = true; // 解決済み説明文を表示するかどうか

    // プレビュー用の変数
    private WeaponEntity previewWeapon;
    private bool showPreview = false;
    private Vector2 previewScrollPos;

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
    /// 属性の日本語表示名を取得
    /// </summary>
    private string GetAttributeDisplayName(WeaponEntity.Attribute attribute)
    {
        switch (attribute)
        {
            case WeaponEntity.Attribute.Slash:
                return "斬";
            case WeaponEntity.Attribute.Blunt:
                return "鈍";
            case WeaponEntity.Attribute.Pierce:
                return "突";
            case WeaponEntity.Attribute.Bullet:
                return "弾";
            default:
                return attribute.ToString();
        }
    }
    /// <summary>
    /// 説明文のプレースホルダーを実際の値に置換
    /// </summary>
    private string GetResolvedDescription(WeaponEntity weapon)
    {
        if (string.IsNullOrEmpty(weapon.WeaponDescription))
            return "";

        return weapon.WeaponDescription
            .Replace("{Name}", weapon.WeaponName)
            .Replace("{Attack}", weapon.WeaponAttackPower.ToString())
            .Replace("{Attribute}", GetAttributeDisplayName(weapon.WeaponAttribute))
            .Replace("{Peaky}", weapon.PeakyCoefficient.ToString());
    }

    /// <summary>
    /// 武器プレビューの表示
    /// </summary>
    private void DrawWeaponPreview(WeaponEntity weapon)
    {
        if (weapon == null) return;

        EditorGUILayout.BeginVertical("box", GUILayout.Width(300), GUILayout.Height(400));

        // 武器名
        EditorGUILayout.LabelField("武器名", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(weapon.WeaponName, EditorStyles.largeLabel);

        EditorGUILayout.Space();

        // アイコン表示
        if (weapon.WeaponIcon != null)
        {
            EditorGUILayout.LabelField("アイコン", EditorStyles.boldLabel);
            Rect iconRect = GUILayoutUtility.GetRect(64, 64, GUILayout.Width(64), GUILayout.Height(64));
            GUI.DrawTexture(iconRect, weapon.WeaponIcon.texture);
        }

        EditorGUILayout.Space();

        // 属性とカテゴリ
        EditorGUILayout.LabelField("属性", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(GetAttributeDisplayName(weapon.WeaponAttribute));

        EditorGUILayout.Space();

        // 説明文
        EditorGUILayout.LabelField("説明文", EditorStyles.boldLabel);
        string resolvedDesc = GetResolvedDescription(weapon);
        EditorGUILayout.TextArea(resolvedDesc, EditorStyles.wordWrappedLabel, GUILayout.Height(100));

        EditorGUILayout.Space();

        // 詳細情報
        EditorGUILayout.LabelField("詳細情報", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"ID: {weapon.WeaponID}");
        EditorGUILayout.LabelField($"攻撃力: {weapon.WeaponAttackPower}");
        EditorGUILayout.LabelField($"特化係数: {weapon.PeakyCoefficient}");

        EditorGUILayout.EndVertical();
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

        // 表示オプション
        EditorGUILayout.BeginHorizontal();
        showResolvedDescription = EditorGUILayout.Toggle("説明文を表示", showResolvedDescription);
        EditorGUILayout.EndHorizontal();

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
        GUILayout.Label("Attack", GUILayout.Width(60));
        GUILayout.Label("Attribute", GUILayout.Width(80));
        GUILayout.Label("Peaky", GUILayout.Width(50));
        GUILayout.Label("Icon", GUILayout.Width(60));
        GUILayout.Label(showResolvedDescription ? "Description" : "Description (生データ)", GUILayout.Width(200));
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
            weapon.PeakyCoefficient = EditorGUILayout.IntField(weapon.PeakyCoefficient, GUILayout.Width(50));
            weapon.WeaponIcon = (Sprite)EditorGUILayout.ObjectField(weapon.WeaponIcon, typeof(Sprite), false, GUILayout.Width(60));

            // 説明文の表示（編集は生データのみ、表示は選択に応じて切り替え）
            EditorGUILayout.BeginVertical(GUILayout.Width(400));

            // 編集用の生データフィールド
            weapon.WeaponDescription = EditorGUILayout.TextArea(weapon.WeaponDescription, GUILayout.Width(500), GUILayout.Height(40));

            // 表示用の解決済みフィールド（読み取り専用）
            if (showResolvedDescription)
            {
                EditorGUILayout.LabelField("表示用:", EditorStyles.miniLabel);
                string resolvedDesc = GetResolvedDescription(weapon);

                // 解決済み説明文を読み取り専用で表示
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextArea(resolvedDesc, GUILayout.Width(500), GUILayout.Height(40));
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.EndVertical();

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
            EditorGUILayout.BeginHorizontal(GUILayout.Width(140));

            // プレビューボタン
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("プレビュー", GUILayout.Width(65)))
            {
                previewWeapon = weapon;
                showPreview = true;
            }
            GUI.backgroundColor = Color.white;

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

        EditorGUILayout.EndVertical();

        // 右側：プレビュー表示
        if (showPreview && previewWeapon != null)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(320));

            // プレビューヘッダー
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("武器プレビュー", EditorStyles.boldLabel);
            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                showPreview = false;
                previewWeapon = null;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // プレビュー表示
            previewScrollPos = EditorGUILayout.BeginScrollView(previewScrollPos);
            DrawWeaponPreview(previewWeapon);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndHorizontal();

        // 統計情報
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"合計: {weaponList.Count} 個の武器", EditorStyles.miniLabel);

        // プレースホルダー使用例の表示
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("使用可能なプレースホルダー:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("  {Name} - 武器の名前", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("  {Attack} - 攻撃力", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("  {Attribute} - 属性（斬、鈍、突、弾）", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("  {Category} - カテゴリ（片手剣、両手剣など）", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("  {Peaky} - 特化係数", EditorStyles.miniLabel);

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
        newWeapon.PeakyCoefficient = 0;
        newWeapon.WeaponDescription = "{Name}は{Attribute}属性の{Category}で、攻撃力{Attack}、特化係数{Peaky}を持つ。";

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