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

            playerUIs[pIndex].SetHighlight(new Color(0.5f, 0.8f, 1f));  // 誰のターンか分かりやすくする

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

                int currentTargetIndex = 0;
                int previousTargetIndex = 0;

                EnemyStatusUIController targetUI = enemyUIs.FirstOrDefault(ui => ui.GetEnemyModel() == livingEnemies[currentTargetIndex]);
                if (targetUI != null)
                {
                    targetUI.SetHighlight(new Color(1f, 0.5f, 0.5f)); // 赤色
                }

                // 選択が確定するまで無限ループ
                while (true)
                {
                    yield return null;

                    bool selectionChanged = false;
                    if (Input.GetKeyDown(KeyCode.RightArrow))
                    {
                        currentTargetIndex = (currentTargetIndex + 1) % livingEnemies.Count;
                        selectionChanged = true;
                    }
                    else if (Input.GetKeyDown(KeyCode.LeftArrow))
                    {
                        currentTargetIndex = (currentTargetIndex - 1 + livingEnemies.Count) % livingEnemies.Count;
                        selectionChanged = true;
                    }

                    if (selectionChanged)
                    {
                        // 前のターゲットのハイライトをリセット
                        EnemyModel prevModel = livingEnemies[previousTargetIndex];
                        EnemyStatusUIController prevUI = enemyUIs.FirstOrDefault(ui => ui.GetEnemyModel() == prevModel);
                        if (prevUI != null)
                        {
                            prevUI.ResetHighlight();
                        }

                        // 新しいターゲットをハイライト
                        EnemyModel currentModel = livingEnemies[currentTargetIndex];
                        EnemyStatusUIController currentUI = enemyUIs.FirstOrDefault(ui => ui.GetEnemyModel() == currentModel);
                        if (currentUI != null)
                        {
                            currentUI.SetHighlight(new Color(1f, 0.5f, 0.5f)); // 赤色
                        }
                        previousTargetIndex = currentTargetIndex;
                    }

                    if (Input.GetKeyDown(KeyCode.Return))
                    {
                        PlayerSelections[currentPlayer].Add(livingEnemies[currentTargetIndex]);

                        // 決定したら全敵のハイライトをリセット
                        foreach (var eUI in enemyUIs)
                        {
                            eUI.ResetHighlight();
                        }
                        break;
                    }
                }
            }
            playerUIs[pIndex].ResetHighlight();
        }

        FinishSelectTurn();
    }

    private void FinishSelectTurn()
    {
        SelectTurnFinished.Invoke();
    }
}