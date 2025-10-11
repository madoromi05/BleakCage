/// <summary>
/// 攻撃先優先順位選択
///</summary>
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class SelectTurn : MonoBehaviour
{
    public Dictionary<PlayerRuntime, List<EnemyModel>> PlayerSelections { get; private set; }

    private List<PlayerRuntime> currentParty;
    private List<EnemyModel> currentEnemies;
    private List<PlayerStatusUIController> playerUIs;
    private List<EnemyStatusUIController> enemyUIs;

    public event System.Action SelectTurnFinished;

    /// <summary>
    /// 選択ターンの初期化
    /// </summary>

    public void StartSelectTurn(List<PlayerRuntime> players, List<EnemyModel> enemies, List<PlayerStatusUIController> pUIs, List<EnemyStatusUIController> eUIs)
    {
        // データ初期化
        this.currentParty = players;
        this.currentEnemies = enemies;
        this.playerUIs = pUIs;
        this.enemyUIs = eUIs;

        PlayerSelections = new Dictionary<PlayerRuntime, List<EnemyModel>>();
        foreach (var player in currentParty)
        {
            PlayerSelections[player] = new List<EnemyModel>();
        }

        Debug.Log("選択データの初期化完了");
        // 選択プロセスを開始
        StartCoroutine(SelectionProcessCoroutine());
    }

    /// <summary>
    /// 実際の選択処理を行うコルーチン
    /// </summary>
    private IEnumerator SelectionProcessCoroutine()
    {
        Debug.Log($"選択ターンのプレイヤー数: {currentParty.Count}");
        // プレイヤー人数分の選択ループ
        for (int pIndex = 0; pIndex < currentParty.Count; pIndex++)
        {
            Debug.Log($"選択ターン");
            PlayerRuntime currentPlayer = currentParty[pIndex];
            if (pIndex >= playerUIs.Count) continue;

            playerUIs[pIndex].StartFlashing(Color.blue); // 誰のターンか分かりやすくする

            // 優先順位3つ分の選択ループ
            for (int priority = 1; priority <= currentParty.Count; priority++)
            {
                Debug.Log($"Player {pIndex + 1} の 優先順位 {priority} を選択してください。(矢印キーで選択、Enterキーで決定)");

                // 生きている敵のリストを毎回取得する
                var livingEnemies = currentEnemies.Where(e => e.EnemyHP > 0).ToList();
                if (livingEnemies.Count == 0)
                {
                    Debug.LogWarning("選択可能な敵がいません。");
                    break; // このプレイヤーの選択を中断
                }

                int currentTargetIndex = 0; // 現在選択している敵のインデックス

                // 選択が確定するまで無限ループ
                while (true)
                {
                    // 全ての敵UIの点滅を一旦停止
                    foreach (var eUI in enemyUIs) eUI.StopFlashing();

                    // 現在選択中の敵のUIだけを点滅させる
                    // EnemyModelから対応するUIを見つける必要がある
                    EnemyModel selectedEnemyModel = livingEnemies[currentTargetIndex];
                    EnemyStatusUIController targetUI = enemyUIs.FirstOrDefault(ui => ui.GetEnemyModel() == selectedEnemyModel);
                    if (targetUI != null)
                    {
                        targetUI.StartFlashing(Color.red);
                    }

                    // 1フレーム待機して、次の入力を受け付ける
                    yield return null;

                    // 右矢印キーでターゲットを次に
                    if (Input.GetKeyDown(KeyCode.RightArrow))
                    {
                        currentTargetIndex = (currentTargetIndex + 1) % livingEnemies.Count;
                    }
                    // 左矢印キーでターゲットを前に
                    else if (Input.GetKeyDown(KeyCode.LeftArrow))
                    {
                        currentTargetIndex = (currentTargetIndex - 1 + livingEnemies.Count) % livingEnemies.Count;
                    }
                    // Enterキーで決定
                    else if (Input.GetKeyDown(KeyCode.Return))
                    {
                        // 選択を登録
                        EnemyModel finalSelectedEnemy = livingEnemies[currentTargetIndex];
                        PlayerSelections[currentPlayer].Add(finalSelectedEnemy);
                        Debug.Log($"Player {currentPlayer.PlayerModel.PlayerName} が 優先度{priority} で {finalSelectedEnemy.EnemyName} を選択");

                        // 全ての敵UIの点滅を停止
                        foreach (var eUI in enemyUIs) eUI.StopFlashing();

                        break; // whileループを抜けて次の優先順位の選択へ
                    }
                }
            }
            playerUIs[pIndex].StopFlashing();
        }

        FinishSelectTurn();
    }

    private void FinishSelectTurn()
    {
        SelectTurnFinished.Invoke();
    }
}