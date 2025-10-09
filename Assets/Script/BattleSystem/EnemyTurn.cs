/// <summary>
/// 敵ターンの処理
///</summary>

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class EnemyTurn : MonoBehaviour
{
    private PlayerModel playerModel;                        // プレイヤーModelデータ
    private EnemyModel enemyModel;                          // 敵Modelデータ
    private List<PlayerModel> players;                      // プレイヤー配列
    private List<EnemyModel> enemys;                        // 敵配列
    private IEnemyAttackStrategy damageStrategy;
    private Queue<ICommand> commandQueue = new();           // コマンドキュー

    public event System.Action TurnFinished;           // ターン終了イベント

    private int enemycount;
    private int playercount = 1;

    private void Awake()
    {
        damageStrategy = new EnemyAttackDamage();
    }
    
    public void EnemySetup(List<PlayerModel> players, List<EnemyModel> enemys)
    {
        this.players = players;
        this.enemys = enemys;
    }

    public void StartEnemyTurn()
    {
        enemycount = enemys.Count;
        playercount = players.Count;
        commandQueue.Clear();
        StartCoroutine(Attack());
    }
    
    /// <summary>
    /// 攻撃先選択
    /// </summary>
    private void Choice()
    {
        int choice;
        while(true)
        {
            choice = Random.Range(1, playercount + 1);
            if(players[choice] == null) continue;
            break;
        }
        playerModel = players[choice];
    }

    /// <summary>
    /// コマンドパターン呼び出し
    /// </summary>
    private void Battle()
    {
        for(int enemyattacker = 1; enemyattacker < enemycount; enemyattacker++)
        {
            enemyModel = enemys[enemyattacker];
            if (enemyModel == null)
            {
                Debug.LogError($"敵はいません");
                continue;
            }
            while (true)
            {
                Choice();
                if (playerModel != null)
                {
                    break;
                }
                Debug.LogError($"相手はいません");
            }
            commandQueue.Enqueue(new EnemyAttackCommand(playerModel, enemyModel, damageStrategy));
        }
    }
    
    private IEnumerator Attack()
    {
        Battle();
        // 順に実行
        while (commandQueue.Count > 0)
        {
            var command = commandQueue.Dequeue();
            command.Do();
            yield return new WaitForSeconds(0.3f); // 任意のウェイト
        }
        // ターン終了イベントを発火
        TurnFinished?.Invoke();
    }
}
