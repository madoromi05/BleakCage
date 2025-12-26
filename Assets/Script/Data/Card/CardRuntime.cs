using System;
/// <summary>
/// 動的または、UUIDでカードを管理するためのクラス
/// </summary>
public class CardRuntime : IAttackComponent
{
    public int ID { get; private set; }
    public System.Guid InstanceID { get; private set; }
    public WeaponRuntime weaponRuntime { get; set; }
    public AttributeType attribute { get; private set; }
    public CardModel Model { get; private set; }
    public float DefensePenetration { get; private set; }
    private readonly float _outputModifier;

    /// <summary>
    /// Jsonファイルから読み込んだカードのインスタンスを生成するコンストラクタ
    /// </summary>
    public CardRuntime(CardModel model, string instanceID)
    {
        InstanceID = Guid.Parse(instanceID);
        ID = model.ID;
        this.Model = model;
        _outputModifier = model.OutputModifier;
        DefensePenetration = model.DefensePenetration;
        this.attribute = model.Attribute;
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
    public PlayerRuntime GetOwnerPlayer()
    {
        if (this.weaponRuntime == null) return null;
        return this.weaponRuntime.ParentPlayer;
    }
}