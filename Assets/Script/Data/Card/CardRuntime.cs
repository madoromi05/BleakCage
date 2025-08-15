/// <summary>
/// カードの動的な値を扱うためのクラス
/// </summary>
public class CardRuntime : IAttackComponent
{
    private readonly float _outputModifier;
    public System.Guid InstanceId { get; private set; }         //おなじCardIDを識別するためのもの
    public int ID { get; private set; }                         //Cardの種類特定
    public string Name { get; private set; }
    public WeaponRuntime weaponRuntime { get; private set; }     // カードが装着されている武器への参照

    public CardRuntime(CardModel model)
    {
        InstanceId = System.Guid.NewGuid();
        ID = model.ID;
        Name = model.Name;
        _outputModifier = model.OutputModifier;
    }

    public float GetPower()
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