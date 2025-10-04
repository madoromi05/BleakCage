/// <summary>
/// 攻撃先優先順位選択
///</summary>
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class SelectTurn : MonoBehaviour
{
    [SerializeField] private EnemyDrop[] enemyDrops;

    public List<EnemyModel> SelectPlayer1 = new List<EnemyModel>();
    public List<EnemyModel> SelectPlayer2 = new List<EnemyModel>();
    public List<EnemyModel> SelectPlayer3 = new List<EnemyModel>();

    private int currentPlayerIndex = 0;   
    private int currentPriority = 1;      

    public event System.Action SelectTurnFinished;

    private void OnEnable()
    {
        foreach (var drop in enemyDrops)
        {
            Debug.Log($"イベント購読: {drop.name}");
            if (drop != null)
                drop.OnEnemyDropped += SelectGet;
        }
    }

    private void OnDisable()
    {
        foreach (var drop in enemyDrops)
        {
            if (drop != null)
                drop.OnEnemyDropped -= SelectGet;
        }
    }
    private void SelectGet(PlayerDrag playerDrag, EnemyModel enemy)
    {
        Debug.Log($"SelectGet受信: {playerDrag.PlayerData.PlayerName} -> {enemy.EnemyName}");
        RegisterSelection(playerDrag.PlayerData, enemy);
    }

    public void StartSelectTurn(List<PlayerRuntime> players, List<EnemyModel> enemies)
    {
        SelectPlayer1.Clear();
        SelectPlayer2.Clear();
        SelectPlayer3.Clear();
        currentPlayerIndex = 0;
        currentPriority = 1;
        enemyDrops = new EnemyDrop[enemies.Count];

        for (int i = 0; i < enemies.Count; i++)
        {
            enemyDrops[i] = enemies[i].EnemyDrop;
        }
    }
    private void FinishSelectTurn()
    {
        SelectTurnFinished.Invoke();
    }

    private void RegisterSelection(PlayerModel player, EnemyModel enemy)
    {
        switch (currentPlayerIndex)
        {
            case 0: SelectPlayer1.Add(enemy); break;
            case 1: SelectPlayer2.Add(enemy); break;
            case 2: SelectPlayer3.Add(enemy); break;
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