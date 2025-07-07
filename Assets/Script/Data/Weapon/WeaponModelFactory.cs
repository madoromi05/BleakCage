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
    /// WeaponEntityから直接WeaponModelを生成
    /// </summary>
    /// <param name="weaponEntity">WeaponEntity</param>
    /// <returns>WeaponModel。生成に失敗した場合はnull</returns>
    public WeaponModel CreateFromEntity(WeaponEntity weaponEntity)
    {
        if (weaponEntity == null)
        {
            Debug.LogError("WeaponEntity is null");
            return null;
        }
        return new WeaponModel(weaponEntity);
    }

    /// <summary>
    /// 複数のWeaponModelを一括生成
    /// </summary>
    /// <param name="weaponIds">武器IDの配列</param>
    /// <returns>WeaponModelの配列（失敗したものはnull）</returns>
    public WeaponModel[] CreateMultipleFromIds(int[] weaponIds)
    {
        if (weaponIds == null || weaponIds.Length == 0)
        {
            Debug.LogWarning("WeaponIds array is null or empty");
            return new WeaponModel[0];
        }

        WeaponModel[] weaponModels = new WeaponModel[weaponIds.Length];
        for (int i = 0; i < weaponIds.Length; i++)
        {
            weaponModels[i] = CreateFromId(weaponIds[i]);
        }
        return weaponModels;
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