/// <summary>
/// 敵ターンの処理
///</summary>

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class EnemyTurn : MonoBehaviour
{
    private List<PlayerModel> players;                      // プレイヤー配列
    private List<EnemyModel> enemies;                       // 敵配列
    private IEnemyAttackStrategy damageStrategy;
    private Queue<ICommand> commandQueue = new();           // コマンドキュー
    private List<PlayerStatusUIController> playerStatusUIControllers;

    public event System.Action TurnFinished;                // ターン終了イベント

    private int enemycount;
    private int playercount = 1;

    private void Awake()
    {
        damageStrategy = new EnemyAttackDamage();
    }
    
    public void EnemySetup(List<PlayerModel> players, List<EnemyModel> enemys, 
                        List<PlayerStatusUIController>playerStatusUIControllers )
    {
        this.players = players;
        this.enemies = enemys;
        this.playerStatusUIControllers = playerStatusUIControllers;
    }

    public void StartEnemyTurn()
    {
        commandQueue.Clear();
        StartCoroutine(ProcessEnemyActions());
    }

    /// <summary>
    /// 攻撃対象となる生存プレイヤーをランダムに選択する
    /// </summary>
    /// <returns>生存しているプレイヤーモデル。いなければnull。</returns>
    private PlayerModel GetRandomLivingPlayer()
    {
        // HPが0より大きいプレイヤーのリストを作成
        var livingPlayers = players.Where(p => p != null && p.PlayerHP > 0).ToList();

        // 生存プレイヤーがいれば、その中からランダムに1人を選択して返す
        if (livingPlayers.Any())
        {
            int choice = Random.Range(0, livingPlayers.Count);
            return livingPlayers[choice];
        }

        // 生存プレイヤーがいない場合はnullを返す
        return null;
    }

    /// <summary>
    /// 敵の攻撃コマンドを準備する
    /// </summary>
    private void PrepareAttackCommands()
    {
        // 生存している敵キャラクターがそれぞれ攻撃を行う
        foreach (var attacker in enemies)
        {
            // HPが0以下の敵は行動しない
            if (attacker == null || attacker.EnemyHP <= 0)
            {
                continue;
            }

            // 攻撃対象となるプレイヤーをランダムに選択
            PlayerModel target = GetRandomLivingPlayer();

            // 攻撃対象が見つかった場合、攻撃コマンドをキューに追加
            if (target != null)
            {
                int targetIndex = players.FindIndex(p => p == target);

                // インデックスが見つかった場合（通常は見つかるはず）、対応するUIコントローラーを取得
                if (targetIndex != -1)
                {
                    PlayerStatusUIController targetUIController = playerStatusUIControllers[targetIndex];
                    commandQueue.Enqueue(new EnemyAttackCommand(target, attacker, damageStrategy, targetUIController));
                }
                else
                {
                    Debug.LogError("ターゲットプレイヤーに対応するUIコントローラーが見つかりませんでした。");
                }
            }
            else
            {
                // 攻撃対象がいない場合（全滅した場合など）は処理を中断
                Debug.LogWarning("攻撃対象となる生存プレイヤーがいません。");
                break;
            }
        }
    }

    /// <summary>
    /// 敵の行動を順次実行するコルーチン
    /// </summary>
    private IEnumerator ProcessEnemyActions()
    {
        // 実行する攻撃コマンドを準備
        PrepareAttackCommands();

        // キューにたまったコマンドを順に実行
        while (commandQueue.Count > 0)
        {
            var command = commandQueue.Dequeue();
            command.Do();
            yield return new WaitForSeconds(0.3f); // 攻撃ごとのウェイト
        }

        // ターン終了イベントを発火
        TurnFinished?.Invoke();
    }
}
