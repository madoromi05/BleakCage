using System;
/// <summary>
/// 動的または、UUIDでカードを管理するためのクラス
/// </summary>
public class CardRuntime : IAttackComponent
{
    public int ID { get; private set; }
    public System.Guid InstanceID { get; private set; }
    public WeaponRuntime weaponRuntime { get; private set; }     // カードが装着されている武器への参照
    public AttributeType attribute { get; private set; }
    public float DefensePenetration { get; private set; }
    private readonly float _outputModifier;
    public CardRuntime(CardModel model)
    {
        InstanceID = System.Guid.NewGuid();
        ID = model.ID;
        _outputModifier = model.OutputModifier;
        attribute = model.Attribute;
        DefensePenetration = model.DefensePenetration;
    }

    /// <summary>
    /// Jsonファイルから読み込んだカードのインスタンスを生成するコンストラクタ
    /// </summary>
    public CardRuntime(CardModel model, string instanceID)
    {
        InstanceID = Guid.Parse(instanceID);
        ID = model.ID;
        _outputModifier = model.OutputModifier;
        DefensePenetration = model.DefensePenetration;
    }
    public float GetOutput()
    {
        return _outputModifier;
    }

    /// <summary>
    /// 親オブジェクト（武器）への参照を設定する内部メソッド
    /// </summary>
    internal void SetParent(WeaponRuntime weapon)
    {
        weaponRuntime = weapon;
    }
}