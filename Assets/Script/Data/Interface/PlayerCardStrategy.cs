/// <summary>
/// 特定キャラクター専用カード
/// </summary>
public class CharacterOnlyCardStrategy : ICardRestrictionStrategy
{
    public int characterId;  // 対応キャラのID

    public CharacterOnlyCardStrategy(int id)
    {
        characterId = id;
    }

    public bool IsUsableBy(PlayerEntity character, WeaponEntity weapon)
    {
        return character.PlayerId == characterId;
    }
}
