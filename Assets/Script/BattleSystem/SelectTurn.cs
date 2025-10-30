using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SelectTurn : MonoBehaviour, IPhase
{
    [SerializeField] private SelectInputReader selectInputReader;
    public Dictionary<PlayerRuntime, List<EnemyModel>> PlayerSelections { get; private set; }

    public event System.Action SelectTurnFinished;
    public event System.Action OnPhaseFinished;

    private void OnUpPressed() { upPressed = true; }
    private void OnDownPressed() { downPressed = true; }
    private void OnConfirmPressed() { confirmPressed = true; }
    private AudioSource audioSource;
    public AudioClip check;

    private int livingEnemyCount;
    private int currentTargetIndex;
    private int previousTargetIndex;

    private List<PlayerRuntime> _currentPlayers;
    private List<EnemyModel> _currentEnemies;
    private List<PlayerStatusUIController> _playerUIs;
    private List<EnemyStatusUIController> _enemyUIs;
    private bool upPressed = false;
    private bool downPressed = false;
    private bool confirmPressed = false;
    private bool isKeepingSelections = false;

    public void Initialize(List<PlayerRuntime> players, List<EnemyModel> enemies, List<PlayerStatusUIController> pUIs, List<EnemyStatusUIController> eUIs)
    {
        _currentPlayers = players;
        _currentEnemies = enemies;
        _playerUIs = pUIs;
        _enemyUIs = eUIs;

        PlayerSelections = new Dictionary<PlayerRuntime, List<EnemyModel>>();
        foreach (var player in _currentPlayers)
        {
            PlayerSelections[player] = new List<EnemyModel>();
        }
        Debug.Log("選択データの初期化完了");
        // 選択プロセスを開始
        audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// 保持している全てのプレイヤーの選択リストをクリアする
    /// </summary>
    public void ClearSelections()
    {
        if (PlayerSelections == null)
        {
            // 念のため初期化
            PlayerSelections = new Dictionary<PlayerRuntime, List<EnemyModel>>();
            foreach (var player in _currentPlayers)
            {
                PlayerSelections[player] = new List<EnemyModel>();
            }
            return;
        }

        // 既存のリストをクリアする
        foreach (var player in _currentPlayers)
        {
            if (PlayerSelections.ContainsKey(player))
            {
                PlayerSelections[player].Clear();
            }
            else
            {
                PlayerSelections[player] = new List<EnemyModel>();
            }
        }
        Debug.Log("全プレイヤーのターゲット選択がクリアされました。");
    }

    public void StartPhase()
    {
        StartCoroutine(SelectionProcessCoroutine(isKeepingSelections));
        isKeepingSelections = false;
    }

    /// <summary>
    /// 継続モードを指定してフェーズを開始するための公開メソッド
    /// </summary>
    public void StartPhase(bool keepSelections)
    {
        isKeepingSelections = keepSelections;
        StartPhase(); // IPhase の StartPhase を呼び出す
    }
    /// <summary>
    /// 通常の選択処理を行うコルーチン
    /// </summary>
    private IEnumerator SelectionProcessCoroutine(bool keepSelections)
    {
        Debug.Log($"選択ターンのプレイヤー数: {_currentPlayers.Count}");
        // プレイヤー人数分の選択ループ
        for (int pIndex = 0; pIndex < _currentPlayers.Count; pIndex++)
        {
            PlayerRuntime currentPlayer = _currentPlayers[pIndex];
            if (pIndex >= _playerUIs.Count) continue;

            if (!keepSelections)
            {
                _playerUIs[pIndex].SetHighlight(new Color(0.5f, 0.8f, 1f));
            }

            int currentLivingEnemies = _currentEnemies.Count(e => e.EnemyHP > 0);
            if (currentLivingEnemies == 0)
            {
                Debug.LogWarning("選択可能な敵がいないため、次のプレイヤーに進みます。");
                _playerUIs[pIndex].ResetHighlight();
                continue;
            }

            // 優先順位分の選択ループ
            for (int priority = 1; priority <= _currentPlayers.Count; priority++)
            {
                Debug.Log($"Player {pIndex + 1} の 優先順位 {priority} を選択してください。(矢印キーで選択、Enterキーで決定)");
                // 敵の生存数をチェックする変数
                livingEnemyCount = _currentEnemies.Count(e => e.EnemyHP > 0);
                if (livingEnemyCount == 0)
                {
                    Debug.LogWarning("選択可能な敵がいないため、選択をスキップします。");
                    break;
                }

                int alreadySelectedCount = PlayerSelections[currentPlayer].Count;
                if (alreadySelectedCount >= livingEnemyCount)
                {
                    Debug.LogWarning($"Player {currentPlayer.PlayerModel.PlayerName} は全ての生存している敵を選択済みです。");
                    break;
                }

                yield return StartCoroutine(SelectOneTargetCoroutine(currentPlayer, priority, (selectedEnemy) =>
                {
                    Debug.Log($"Player {currentPlayer.PlayerModel.PlayerName} が 優先度{priority} で {selectedEnemy.EnemyName} を選択");
                }, keepSelections));
            }
            _playerUIs[pIndex].ResetHighlight();

            // 全ての敵が倒されたら、選択フェーズを即時終了
            if (livingEnemyCount == 0)
            {
                Debug.Log("全ての敵が倒されました。");
                break;
            }
        }

        FinishSelectTurn();
        OnPhaseFinished?.Invoke();
    }

    /// <summary>
    /// プレイヤー1人が敵1体を選択するためのコルーチン
    /// チュートリアルからも呼び出せるように public にする
    /// </summary>
    public IEnumerator SelectOneTargetCoroutine(PlayerRuntime player, int priority, System.Action<EnemyModel> onSelected, bool keepSelections = false)
    {
        Debug.Log($"Player {player.PlayerModel.PlayerName} の 優先順位 {priority} を選択してください。(矢印キーで選択、Enterキーで決定)");

        var livingEnemies = _currentEnemies.Where(e => e.EnemyHP > 0).ToList();
        int livingEnemyCount = livingEnemies.Count;

        if (livingEnemies.Count == 0)
        {
            Debug.LogWarning("選択可能な敵がいません。");
            yield break;
        }

        currentTargetIndex = 0;
        previousTargetIndex = 0;

        // 最初のターゲットUIをハイライト
        EnemyStatusUIController targetUI = _enemyUIs.FirstOrDefault(ui => ui.GetEnemyModel() == livingEnemies[currentTargetIndex]);
        if (targetUI != null && !keepSelections)
        {
            targetUI.SetHighlight(new Color(1f, 0.5f, 0.5f));
        }

        selectInputReader.UpStatusEvent   += OnUpPressed;
        selectInputReader.DownStatusEvent += OnDownPressed;
        selectInputReader.ConfirmEvent    += OnConfirmPressed;

        upPressed = false;
        downPressed = false;
        confirmPressed = false;

        bool selectionMade = false;
        while (!selectionMade)
        {
            yield return null;

            bool selectionChanged = false;
            if (downPressed)
            {
                currentTargetIndex = (currentTargetIndex + 1) % livingEnemies.Count;
                selectionChanged = true;
                downPressed = false;
            }
            else if (upPressed)
            {
                currentTargetIndex = (currentTargetIndex - 1 + livingEnemies.Count) % livingEnemies.Count;
                selectionChanged = true;
                upPressed = false;
            }

            if (selectionChanged)
            {
                if (!keepSelections)
                {
                    EnemyModel prevModel = livingEnemies[previousTargetIndex];
                    EnemyStatusUIController prevUI = _enemyUIs.FirstOrDefault(ui => ui.GetEnemyModel() == prevModel);
                    if (prevUI != null) prevUI.ResetHighlight();

                    EnemyModel currentModel = livingEnemies[currentTargetIndex];
                    EnemyStatusUIController currentUI = _enemyUIs.FirstOrDefault(ui => ui.GetEnemyModel() == currentModel);
                    if (currentUI != null) currentUI.SetHighlight(new Color(1f, 0.5f, 0.5f));
                }

                previousTargetIndex = currentTargetIndex;
            }

            if (confirmPressed)
            {
                confirmPressed = false;
                EnemyModel selectedEnemy = livingEnemies[currentTargetIndex];

                if (PlayerSelections[player].Contains(selectedEnemy))
                {
                    Debug.Log("その敵は既に選択済みです。別の敵を選択してください。");
                    continue;
                }

                PlayerSelections[player].Add(selectedEnemy);
                onSelected?.Invoke(selectedEnemy);

                foreach (var eUI in _enemyUIs) eUI.ResetHighlight();

                selectionMade = true; // whileループを抜ける
            }
        }
        selectInputReader.UpStatusEvent -= OnUpPressed;
        selectInputReader.DownStatusEvent -= OnDownPressed;
        selectInputReader.ConfirmEvent -= OnConfirmPressed;
    }
    /// <summary>
    /// チュートリアル完了後、残りの選択データを自動で設定する
    /// </summary>
    public void FinalizeSelectionsForTutorial()
    {
        // チュートリアルで選択されなかったプレイヤーの選択データを自動で（ダミーで）設定する
        var livingEnemies = _currentEnemies.Where(e => e.EnemyHP > 0).ToList();
        int livingEnemyCount = livingEnemies.Count;
        if (livingEnemyCount == 0) return;

        foreach (var player in _currentPlayers)
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