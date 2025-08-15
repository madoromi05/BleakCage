// PlayerDataModels.cs

using System;
using System.Collections.Generic;

[Serializable]
public class CardData
{
    public string InstanceId; // このカードインスタンス固有のID
    public int CardId;        // カードのベースID (例: CardEntityから)
}

[Serializable]
public class WeaponData
{
    public string InstanceId; // この武器インスタンス固有のID
    public int WeaponId;      // 武器のベースID (WeaponEntity/Modelから)
    public List<CardData> SlottedCards; // この武器に装備されているカード
}

[Serializable]
public class CharacterData
{
    public string InstanceId; // このキャラクターインスタンス固有のID
    public int CharacterId;   // キャラクターのベースID (PlayerEntity/Modelから)
    public List<WeaponData> EquippedWeapons; // このキャラクターが装備している武器
}

[Serializable]
public class PlayerProfile
{
    public string PlayerName;
    public List<CharacterData> BattleCharacters; // 現在のバトルに出撃している3人のキャラクター
    // 控えのキャラクターリストなどをここに追加することも可能
}