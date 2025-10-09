/// <summary>
/// 攻撃先優先順位選択
///</summary>
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.GraphView.GraphView;

public class SelectTurn : MonoBehaviour
{
    public Dictionary<int, List<EnemyModel>> PlayerSelections { get; private set; }

    private int currentPlayerIndex;   
    private int currentPriority;
    private int totalPlayers;
    private int totalEnemies;

    public event System.Action SelectTurnFinished;

    /// <summary>
    /// 選択ターンの初期化
    /// </summary>

    public void StartSelectTurn(List<PlayerRuntime> players, List<EnemyModel> enemies)
    {
        PlayerSelections = new Dictionary<int, List<EnemyModel>>();
        totalPlayers = players.Count;
        totalEnemies = enemies.Count;

        // プレイヤーの人数分、空の選択リストを準備
        for (int i = 0; i < totalPlayers; i++)
        {
            PlayerSelections[i] = new List<EnemyModel>();
        }
        currentPlayerIndex = 0;
        currentPriority = 1;
        Debug.Log("選択データの初期化完了");
    }
    private void FinishSelectTurn()
    {
        SelectTurnFinished.Invoke();
    }

    private void RegisterSelection(PlayerModel player, EnemyModel enemy)
    {
        if (PlayerSelections.ContainsKey(currentPlayerIndex))
        {
            PlayerSelections[currentPlayerIndex].Add(enemy);
        }

        Debug.Log($"Player{player.PlayerName} が {enemy.EnemyName} を選択");

        currentPriority++;

        if (currentPriority > 3)
        {
            currentPriority = 1;
            currentPlayerIndex++;
        }

        if (currentPlayerIndex >= 3)
        {
            Debug.Log("全プレイヤーの選択終了");
            FinishSelectTurn();
        }
    }
}