using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class SelectTurn : MonoBehaviour
{

    private PlayerModel playerModel;                        // プレイヤーRuntimeデータ
    private EnemyModel enemyModel;                          // 敵モデル
    private List<PlayerModel> players;                      // プレイヤー配列
    private List<EnemyModel> enemys;                        // 敵配列

    private DragPerformEvent ka;

    public void SelectSetup(List<PlayerModel> players, List<EnemyModel> enemys)
    {
        this.players = players;
        this.enemys = enemys;
    }
    private void Select()
    {
        
    }
}
