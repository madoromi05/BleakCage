using System.Linq;
using UnityEngine;

/// <summary>
/// WeaponModelを生成するファクトリクラス
/// </summary>
public class WeaponModelFactory
{
    /// <summary>
    /// IDからWeaponModelを生成
    /// </summary>
    public WeaponModel CreateFromId(int weaponId)
    {
        WeaponEntity weaponEntity = LoadWeaponEntity(weaponId);
        if (weaponEntity == null)
        {
            Debug.LogError($"WeaponEntity not found for ID: {weaponId}");
            return null;
        }
        return new WeaponModel(weaponEntity);
    }

    /// <summary>
    /// WeaponEntityを読み込む
    /// フォルダ全体をロードし、名前が一致するものを検索する
    /// </summary>
    private WeaponEntity LoadWeaponEntity(int weaponId)
    {
        // ファイル個別のパスではなく、フォルダのパスを指定
        string folderPath = "EntityDataList/WeaponEntityList";

        // フォルダ内の全データをロード
        WeaponEntity[] allWeapons = Resources.LoadAll<WeaponEntity>(folderPath);
        string exactName = $"Weapon_{weaponId}";
        string prefixName = $"Weapon_{weaponId}_";

        WeaponEntity targetEntity = allWeapons.FirstOrDefault(w =>
            w.name == exactName || w.name.StartsWith(prefixName));

        if (targetEntity == null)
        {
            Debug.LogWarning($"WeaponEntity not found in folder '{folderPath}' for ID: {weaponId} (Expected file name starting with 'Weapon_{weaponId}_...')");
        }

        return targetEntity;
    }
}