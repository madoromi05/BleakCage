using System;
using System.Collections.Generic;

[Serializable]
public class CardData
{
    public string InstanceId;
    public int CardId;
}

[Serializable]
public class WeaponData
{
    public string InstanceId;
    public int WeaponId;
    public List<CardData> SlottedCards;
}

[Serializable]
public class CharacterData
{
    public string InstanceId;
    public int CharacterId;
    public List<WeaponData> EquippedWeapons;
}

[Serializable]
public class PlayerProfile
{
    public string PlayerName;
    public List<CharacterData> BattleCharacters;
}