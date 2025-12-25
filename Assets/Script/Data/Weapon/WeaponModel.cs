using System.Collections.Generic;
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
    public GameObject WeaponPrefab { get; set; }
    public List<CardEntity> DefaultCards { get; private set; }
    public HandPosition HoldHandType { get; private set; }
    /// <summary>
    /// コンストラクタ：指定IDに基づいてResourcesからWeaponEntityを読み込み、モデルに変換する
    /// </summary>
    public WeaponModel(WeaponEntity entity)
    {
        if (entity == null)
        {
            Debug.LogError("weaponEntity is null");
            return;
        }

        ID = (int)entity.ID;
        Name = entity.Name;
        AttackPower = entity.AttackPower;
        Attribute = entity.Attribute;
        PeakyCoefficient = entity.PeakyCoefficient;
        Icon = entity.Icon;
        Description = entity.Description;
        WeaponPrefab = entity.WeaponPrefab;
        DefaultCards = entity.DefaultCards;
        HoldHandType = entity.HoldHandType;
    }

    //runtime用
    public WeaponModel(int Id, string name, float attackPower, AttributeType attribute, float peakyCoefficient, GameObject prefab)
    {
        ID = Id;
        Name = name;
        AttackPower = attackPower;
        Attribute = attribute;
        PeakyCoefficient = peakyCoefficient;
        WeaponPrefab = prefab;
    }
    public WeaponModel(){}
}
