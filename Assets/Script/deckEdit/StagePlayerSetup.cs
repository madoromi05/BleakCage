using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// インスペクター上でStageごとのデッキを設定するためのデータセット
/// </summary>
[System.Serializable]
public class CharacterSetupData
{
    [Header("基本設定")]
    public int CharacterID;
    public int Level;

    [Header("装備武器")]
    public List<WeaponSetupData> Weapons = new List<WeaponSetupData>();
}

/// <summary>
/// 武器と、そのスロットに入れるカードのデータ
/// </summary>
[System.Serializable]
public class WeaponSetupData
{
    public int WeaponID;
}

/// <summary>
/// ステージごとのプレイヤー編成を定義するアセット
/// </summary>
[CreateAssetMenu(fileName = "NewStagePlayerSetup", menuName = "Data/Stage Player Setup")]
public class StagePlayerSetup : ScriptableObject
{
    [Header("このステージでのパーティーメンバー")]
    public List<CharacterSetupData> PartyMembers = new List<CharacterSetupData>();
}