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

    private bool isTutorialMode = false;

    public event System.Action SelectTurnFinished;

    // --- チュートリアル用 ---
    public event System.Action OnTargetSelectedForTutorial;
    // --------------------

    /// <summary>
    /// チュートリアルモードを設定する
    /// </summary>
    public void SetTutorialMode(bool isTutorial)
    {
        this.isTutorialMode = isTutorial;
    }

    public void StartSelectTurn(List<PlayerRuntime> players, List<EnemyModel> enemies, List<PlayerStatusUIController> pUIs, List<EnemyStatusUIController> eUIs)
    {
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

        // チュートリアルモードでなければ、通常の選択コルーチンを開始
        if (!isTutorialMode)
        {
            StartCoroutine(SelectionProcessCoroutine());
        }
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
            if (pIndex >= playerUIs.Count) continue;

            playerUIs[pIndex].SetHighlight(new Color(0.5f, 0.8f, 1f));

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

                EnemyStatusUIController targetUI = enemyUIs.FirstOrDefault(ui => ui.GetEnemyModel() == livingEnemies[currentTargetIndex]);
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
                        EnemyStatusUIController prevUI = enemyUIs.FirstOrDefault(ui => ui.GetEnemyModel() == prevModel);
                        if (prevUI != null) prevUI.ResetHighlight();

                        EnemyModel currentModel = livingEnemies[currentTargetIndex];
                        EnemyStatusUIController currentUI = enemyUIs.FirstOrDefault(ui => ui.GetEnemyModel() == currentModel);
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

                        foreach (var eUI in enemyUIs) eUI.ResetHighlight();

                        // チュートリアル中ならイベントを発火
                        if (isTutorialMode)
                        {
                            OnTargetSelectedForTutorial?.Invoke();
                        }

                        break;
                    }
                }
            }
            playerUIs[pIndex].ResetHighlight();
        }

        FinishSelectTurn();
    }

    /// <summary>
    /// チュートリアル用に外部から呼び出す選択コルーチン
    /// </summary>
    public IEnumerator SelectionProcessCoroutineForTutorial(int playerCount)
    {
        // チュートリアルでは、指定された人数分だけループを回す
        for (int pIndex = 0; pIndex < playerCount; pIndex++)
        {
            // NOTE: 簡潔にするため、SelectionProcessCoroutineのロジックを一部再利用します。
            // 本来は共通のメソッドに切り出すのが望ましいです。
            PlayerRuntime currentPlayer = currentParty[pIndex];
            if (pIndex >= playerUIs.Count) continue;

            playerUIs[pIndex].SetHighlight(new Color(0.5f, 0.8f, 1f));

            // チュートリアルでは優先順位1つだけ選択
            for (int priority = 1; priority <= 1; priority++)
            {
                var livingEnemies = currentEnemies.Where(e => e.EnemyHP > 0).ToList();
                if (livingEnemies.Count == 0) break;

                int currentTargetIndex = 0;
                int previousTargetIndex = 0;

                EnemyStatusUIController targetUI = enemyUIs.FirstOrDefault(ui => ui.GetEnemyModel() == livingEnemies[currentTargetIndex]);
                if (targetUI != null) targetUI.SetHighlight(new Color(1f, 0.5f, 0.5f));

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
                        EnemyStatusUIController prevUI = enemyUIs.FirstOrDefault(ui => ui.GetEnemyModel() == prevModel);
                        if (prevUI != null) prevUI.ResetHighlight();

                        EnemyModel currentModel = livingEnemies[currentTargetIndex];
                        EnemyStatusUIController currentUI = enemyUIs.FirstOrDefault(ui => ui.GetEnemyModel() == currentModel);
                        if (currentUI != null) currentUI.SetHighlight(new Color(1f, 0.5f, 0.5f));

                        previousTargetIndex = currentTargetIndex;
                    }

                    if (Input.GetKeyDown(KeyCode.Return))
                    {
                        EnemyModel selectedEnemy = livingEnemies[currentTargetIndex];
                        if (PlayerSelections[currentPlayer].Contains(selectedEnemy)) continue;

                        PlayerSelections[currentPlayer].Add(livingEnemies[currentTargetIndex]);
                        foreach (var eUI in enemyUIs) eUI.ResetHighlight();

                        if (isTutorialMode) OnTargetSelectedForTutorial?.Invoke();
                        break;
                    }
                }
            }
            playerUIs[pIndex].ResetHighlight();
        }
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
        // チュートリアル中はマネージャーが終了を管理するため、ここでは何もしない
        if (isTutorialMode) return;

        SelectTurnFinished?.Invoke();
    }

    /// <summary>
    /// チュートリアル用に外部からデータを初期化する
    /// </summary>
    public void InitializeForTutorial(List<PlayerRuntime> players, List<EnemyModel> enemies, List<PlayerStatusUIController> pUIs, List<EnemyStatusUIController> eUIs)
    {
        this.currentParty = players;
        this.currentEnemies = enemies;
        this.playerUIs = pUIs;
        this.enemyUIs = eUIs;

        PlayerSelections = new Dictionary<PlayerRuntime, List<EnemyModel>>();
        foreach (var player in currentParty)
        {
            PlayerSelections[player] = new List<EnemyModel>();
        }
        Debug.Log("チュートリアル用の選択データを初期化完了");
    }
}