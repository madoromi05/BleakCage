/// <summary>
/// 汎用カード: すべてのキャラ・武器で使用可能
/// </summary>
public class UniversalCardStrategy : ICardRestrictionStrategy
{
    public bool IsUsableBy(PlayerEntity character, WeaponEntity weapon) => true;
}
