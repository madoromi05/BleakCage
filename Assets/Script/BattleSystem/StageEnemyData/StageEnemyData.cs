using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ステージごとの敵構成を定義するデータアセット
/// </summary>
[CreateAssetMenu(fileName = "StageEnemyData", menuName = "StageEnemeyData")]
public class StageEnemyData : ScriptableObject
{
    public int stageEnemyID;
    public List<int> enemyIDs = new List<int>();
}