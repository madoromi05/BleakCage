using UnityEngine;

public class WeaponModel
{
    public int WeaponId { get; set; }
    public string WeaponName { get; set; }
    public int WeaponAttackPower { get; set; }
    public WeaponEntity.Attribute WeaponAttribute { get; set; }
    public WeaponEntity.WeaponCategory WeaponCategory { get; set; }
    public int PeakyCoefficient { get; set; }
    public string Description { get; set; }
    public Sprite Icon { get; set; }

    // コンストラクタ（武器IDを引数にしてデータを読み込む）
    public WeaponModel(int weaponId)
    {
        // Resourcesフォルダから武器データを取得
        WeaponEntity weaponEntity = Resources.Load<WeaponEntity>("WeaponEntityList/Weapon" + weaponId);

        if (weaponEntity == null)
        {
            Debug.LogError($"WeaponEntity not found for ID: {weaponId}");
            return;
        }

        // 取得したデータをWeaponModelに反映
        WeaponId = weaponEntity.WeaponID;
        WeaponName = weaponEntity.WeaponName;
        WeaponAttackPower = weaponEntity.WeaponAttackPower;
        WeaponAttribute = weaponEntity.WeaponAttribute;
        WeaponCategory = weaponEntity.weaponCategory;
        PeakyCoefficient = weaponEntity.PeakyCoefficient;
        Icon = weaponEntity.WeaponIcon;
        Description = weaponEntity.WeaponDescription;
    }
}