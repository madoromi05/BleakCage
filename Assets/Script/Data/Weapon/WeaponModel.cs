using UnityEngine;

/// <summary>
/// 武器の情報を保持するモデルクラス。リソースからデータをロードして構成される。
/// </summary>
public class WeaponModel
{
    public int WeaponId { get; set; }                         // 武器のID
    public string WeaponName { get; set; }                    // 武器名
    public float WeaponAttackPower { get; set; }              // 攻撃力
    public AttackAttributeType WeaponAttribute { get; set; }  // 属性
    public float PeakyCoefficient { get; set; }               // 特化係数（ピーキー度）
    public string Description { get; set; }                   // 武器の説明文
    public Sprite Icon { get; set; }                          // アイコン画像

    /// <summary>
    /// コンストラクタ：指定IDに基づいてResourcesからWeaponEntityを読み込み、モデルに変換する
    /// </summary>
    public WeaponModel(WeaponEntity weaponEntity)
    {
        //初期化
        if (weaponEntity == null)
        {
            Debug.LogError("weaponEntity is null");
            return;
        }
        
        WeaponId = (int)weaponEntity.WeaponId;
        WeaponName = weaponEntity.WeaponName;
        WeaponAttackPower = weaponEntity.WeaponAttackPower;
        WeaponAttribute = weaponEntity.WeaponAttribute;
        PeakyCoefficient = weaponEntity.WeaponPeakyCoefficient;
        Icon = weaponEntity.WeaponIcon;
        Description = weaponEntity.WeaponDescription;
    }
}
