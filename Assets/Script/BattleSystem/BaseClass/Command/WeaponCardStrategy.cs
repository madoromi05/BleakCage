/// <summary>
/// 特定武器専用カード
/// </summary>
public class WeaponOnlyCardStrategy : ICardRestrictionStrategy
{
    public int weaponId;  // 対応武器のID

    public WeaponOnlyCardStrategy(int id)
    {
        weaponId = id;
    }

    public bool IsUsableBy(PlayerEntity character, WeaponEntity weapon)
    {
        return weapon.WeaponId == weaponId;
    }
}
