using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

/// <summary>
/// 武器の情報を保持するモデルクラス。リソースからデータをロードして構成される。
/// こちらは、静的なデータのみを管理する
/// </summary>
public class WeaponModel
{
    public int ID { get; set; }                         // 武器のID
    public string Name { get; set; }                    // 武器名
    public float AttackPower { get; set; }              // 攻撃力
    public AttributeType Attribute { get; set; }        // 属性
    public float PeakyCoefficient { get; set; }         // 特化係数（ピーキー度）
    public string Description { get; set; }             // 武器の説明文
    public Sprite Icon { get; set; }                    // 武器画像
    public GameObject WeaponPrefab { get; private set; }
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

        ID = (int)weaponEntity.ID;
        Name = weaponEntity.Name;
        AttackPower = weaponEntity.AttackPower;
        Attribute = weaponEntity.Attribute;
        PeakyCoefficient = weaponEntity.PeakyCoefficient;
        Icon = weaponEntity.Icon;
        Description = weaponEntity.Description;
        WeaponPrefab = weaponEntity.WeaponPrefab;
    }

    public WeaponModel(int Id, string name, float attackPower, AttributeType attribute, float peakyCoefficient,GameObject prefab)
    {
        ID = Id;
        Name = name;
        AttackPower = attackPower;
        Attribute = attribute;
        PeakyCoefficient = peakyCoefficient;
        WeaponPrefab = prefab;
    }
}
