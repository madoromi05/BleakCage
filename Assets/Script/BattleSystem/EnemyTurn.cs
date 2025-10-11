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
            choice = Random.Range(0, playercount);
            if(players[choice] == null) continue;
            if (players[choice] != null && players[choice].PlayerHP > 0)
            {
                playerModel = players[choice];
                break; // 有効なターゲットが見つかったらループを抜けます。
            }
        }
    }

    /// <summary>
    /// コマンドパターン呼び出し
    /// </summary>
    private void Battle()
    {
        for(int enemyattacker = 0; enemyattacker < enemycount; enemyattacker++)
        {
            enemyModel = enemys[enemyattacker];
            if (enemyModel == null || enemyModel.EnemyHP <= 0)
            {
                Debug.LogError($"敵のHPは0です");
                continue;
            }
            Choice();

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
