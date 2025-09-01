using UnityEngine;

/// <summary>
/// WeaponModelを生成するファクトリクラス
/// 責任：WeaponEntityの読み込みとWeaponModelの生成
/// </summary>
public class WeaponModelFactory
{
    /// <summary>
    /// IDからWeaponModelを生成
    /// </summary>
    /// <param name="weaponId">武器ID</param>
    /// <returns>WeaponModel。生成に失敗した場合はnull</returns>
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
    /// </summary>
    /// <param name="weaponId">武器ID</param>
    /// <returns>WeaponEntity。見つからない場合はnull</returns>
    private WeaponEntity LoadWeaponEntity(int weaponId)
    {
        string path = $"WeaponEntityList/Weapon_{weaponId}";
        WeaponEntity weaponEntity = Resources.Load<WeaponEntity>(path);

        if (weaponEntity == null)
        {
            Debug.LogWarning($"WeaponEntity not found at path: {path}");
        }

        return weaponEntity;
    }
}