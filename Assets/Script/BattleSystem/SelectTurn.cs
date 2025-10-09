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
        // プレイヤー3人分の選択ループ
        for (int pIndex = 0; pIndex < currentParty.Count; pIndex++)
        {
            PlayerRuntime currentPlayer = currentParty[pIndex];
            // UIが存在しない場合はスキップ
            if (pIndex >= playerUIs.Count) continue;

            playerUIs[pIndex].StartFlashing(Color.blue);

            // 優先順位3つ分の選択ループ
            // --- 修正: 敵の数ではなく、優先順位の数（3回）ループするようにする ---
            for (int priority = 1; priority <= 3; priority++)
            {
                // 選択対象の敵を決める (ここでは単純にインデックスでループ)
                // TODO: ここに矢印キーなどで敵を選択するロジックを追加するとより良くなります

                // 生きている敵の中から選択対象を探す必要がある
                // このループは、プレイヤーがどの敵を何番目の優先度で攻撃するかを決めるためのものです
                // ここでは仮実装として、キー入力で敵を選択するロジックを簡略化します。

                Debug.Log($"Player {pIndex + 1} の 優先順位 {priority} を選択してください。(Enterキーで決定)");

                // UI点滅などの処理
                // (例: 選択可能な敵を点滅させる)
                for (int i = 0; i < enemyUIs.Count; i++)
                {
                    if (currentEnemies[i].EnemyHP > 0) // 生きている敵のみ点滅
                    {
                        enemyUIs[i].StartFlashing(Color.red);
                    }
                }

                // Enterキーが押されるまで待機
                yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Return));

                // 全ての敵UIの点滅を停止
                foreach (var eUI in enemyUIs) eUI.StopFlashing();

                // 選択を登録
                // ここでは仮に0番目の敵を選択したものとして登録します
                EnemyModel selectedEnemy = currentEnemies.FirstOrDefault(e => e.EnemyHP > 0);
                if (selectedEnemy != null)
                {
                    // PlayerRuntime をキーとして選択した敵を追加
                    PlayerSelections[currentPlayer].Add(selectedEnemy);
                    Debug.Log($"Player {currentPlayer.PlayerModel.PlayerName} が 優先度{priority} で {selectedEnemy.EnemyName} を選択");
                }
                else
                {
                    Debug.LogWarning("選択可能な敵がいません。");
                    break; // このプレイヤーの選択を中断
                }
            }
            playerUIs[pIndex].StopFlashing();
        }

        // 全員の選択が終わったら終了処理
        FinishSelectTurn();
    }

    private void FinishSelectTurn()
    {
        SelectTurnFinished.Invoke();
    }
}