/// <summary>
/// カードが使用可能かを判断するための戦略インターフェース
/// </summary>

public interface ICardRestrictionStrategy
{
    /// <summary>
    /// このカードが特定のキャラと武器の組み合わせで使用可能か？
    /// </summary>
    /// <param name="character">装備しようとしているキャラクター</param>
    /// <param name="weapon">装備しようとしている武器</param>
    /// <returns>使用可能かどうか</returns>
    bool IsUsableBy(PlayerEntity character, WeaponEntity weapon);
}