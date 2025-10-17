using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class SelectTurn : MonoBehaviour , IPhase
{
    public Dictionary<PlayerRuntime, List<EnemyModel>> PlayerSelections { get; private set; }

    private List<PlayerRuntime> currentParty;
    private List<EnemyModel> currentEnemies;
    public event System.Action SelectTurnFinished;
    public event System.Action OnPhaseFinished;

    private List<PlayerRuntime> _currentPlayers;
    private List<EnemyModel> _currentEnemies;
    private List<PlayerStatusUIController> _playerUIs;
    private List<EnemyStatusUIController> _enemyUIs;

    public void Initialize(List<PlayerRuntime> players, List<EnemyModel> enemies, List<PlayerStatusUIController> pUIs, List<EnemyStatusUIController> eUIs)
    {
        _currentPlayers = players;
        _currentEnemies = enemies;
        _playerUIs = pUIs;
        _enemyUIs = eUIs;

        PlayerSelections = new Dictionary<PlayerRuntime, List<EnemyModel>>();
        foreach (var player in currentParty)
        {
            PlayerSelections[player] = new List<EnemyModel>();
        }
    }

    public void StartPhase()
    {
        StartCoroutine(SelectionProcessCoroutine());
    }

    /// <summary>
    /// 通常の選択処理を行うコルーチン
    /// </summary>
    private IEnumerator SelectionProcessCoroutine()
    {
        Debug.Log($"選択ターンのプレイヤー数: {currentParty.Count}");
        // プレイヤー人数分の選択ループ
        for (int pIndex = 0; pIndex < currentParty.Count; pIndex++)
        {
            PlayerRuntime currentPlayer = currentParty[pIndex];
            if (pIndex >= _playerUIs.Count) continue;

            _playerUIs[pIndex].SetHighlight(new Color(0.5f, 0.8f, 1f));

            // 優先順位分の選択ループ
            for (int priority = 1; priority <= currentParty.Count; priority++)
            {
                Debug.Log($"Player {pIndex + 1} の 優先順位 {priority} を選択してください。(矢印キーで選択、Enterキーで決定)");

                var livingEnemies = currentEnemies.Where(e => e.EnemyHP > 0).ToList();
                if (livingEnemies.Count == 0)
                {
                    Debug.LogWarning("選択可能な敵がいません。");
                    break;
                }

                int currentTargetIndex = 0;
                int previousTargetIndex = 0;

                EnemyStatusUIController targetUI = _enemyUIs.FirstOrDefault(ui => ui.GetEnemyModel() == livingEnemies[currentTargetIndex]);
                if (targetUI != null)
                {
                    targetUI.SetHighlight(new Color(1f, 0.5f, 0.5f));
                }

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
                        EnemyModel prevModel = livingEnemies[previousTargetIndex];
                        EnemyStatusUIController prevUI = _enemyUIs.FirstOrDefault(ui => ui.GetEnemyModel() == prevModel);
                        if (prevUI != null) prevUI.ResetHighlight();

                        EnemyModel currentModel = livingEnemies[currentTargetIndex];
                        EnemyStatusUIController currentUI = _enemyUIs.FirstOrDefault(ui => ui.GetEnemyModel() == currentModel);
                        if (currentUI != null) currentUI.SetHighlight(new Color(1f, 0.5f, 0.5f));

                        previousTargetIndex = currentTargetIndex;
                    }

                    if (Input.GetKeyDown(KeyCode.Return))
                    {
                        EnemyModel selectedEnemy = livingEnemies[currentTargetIndex];

                        if (PlayerSelections[currentPlayer].Contains(selectedEnemy))
                        {
                            Debug.Log("その敵は既に選択済みです。別の敵を選択してください。");
                            continue;
                        }

                        PlayerSelections[currentPlayer].Add(livingEnemies[currentTargetIndex]);

                        foreach (var eUI in _enemyUIs) eUI.ResetHighlight();

                        break;
                    }
                }
            }
            _playerUIs[pIndex].ResetHighlight();
        }

        FinishSelectTurn();
        OnPhaseFinished?.Invoke();
    }

    /// <summary>
    /// チュートリアル完了後、残りの選択データを自動で設定する
    /// </summary>
    public void FinalizeSelectionsForTutorial()
    {
        // チュートリアルで選択されなかったプレイヤーの選択データを自動で（ダミーで）設定する
        var livingEnemies = currentEnemies.Where(e => e.EnemyHP > 0).ToList();
        if (livingEnemies.Count == 0) return;

        foreach (var player in currentParty)
        {
            // まだ誰も選択していない場合、最初の敵を自動で選択させる
            if (PlayerSelections.ContainsKey(player) && PlayerSelections[player].Count == 0)
            {
                PlayerSelections[player].Add(livingEnemies[0]);
            }
        }
    }

    private void FinishSelectTurn()
    {
        SelectTurnFinished?.Invoke();
    }
}