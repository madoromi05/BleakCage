using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// カード1枚分のデータ（ScriptableObjectではない）
/// </summary>
[System.Serializable]
public class CardAssetData
{
    public int ID;
    public string Name;
    //public CardTypeData Type;
    public int EquipableWeaponID;
    public int CharacterID;
    public AttributeType Attribute;
    public float OutputModifier;
    public float HitRate;
    public float DefensePenetration;
    public int AttackCount;
    public int TargetCount;
    public bool Passive;

    public AssetReferenceT<Sprite> IconRef;

    [TextArea(3, 5)]
    public string Description;
}