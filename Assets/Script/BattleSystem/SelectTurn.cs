using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SelectTurn : MonoBehaviour, IPhase
{
    [SerializeField] private SelectInputReader selectInputReader;
    [SerializeField] private GameObject key1UI;
    [SerializeField] private GameObject key2UI;
    [SerializeField] private GameObject key3UI;
    [SerializeField] private GameObject EnemySelectNumber;

    public Dictionary<int, List<EnemyModel>> PlayerSelections { get; private set; }

    public event System.Action SelectTurnFinished;
    public event System.Action OnPhaseFinished;

    private void OnUpPressed() { upPressed = true; }
    private void OnDownPressed() { downPressed = true; }
    private void OnConfirmPressed() { confirmPressed = true; }

    private int livingEnemyCount;
    private int currentTargetIndex;
    private int previousTargetIndex;
    private List<PlayerRuntime> _currentPlayers;
    private List<EnemyModel> _currentEnemies;
    private List<PlayerStatusUIController> _playerUIs;
    private List<EnemyStatusUIController> _enemyUIs;
    private List<GameObject> _keyUIInstances;
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

        // UIは初期化時に一度だけセットアップする（再戦時などにエラーにならないよう配慮）
        SetupKeyUIInstances();
        ResetKeyUIs();

        // 辞書の初期化（既に存在する場合は維持しないと再初期化で消えてしまうためnullチェック）
        if (PlayerSelections == null)
        {
            PlayerSelections = new Dictionary<int, List<EnemyModel>>();
        }

        // プレイヤーIDのキーを確保
        foreach (var player in _currentPlayers)
        {
            if (!PlayerSelections.ContainsKey(player.ID))
            {
                PlayerSelections[player.ID] = new List<EnemyModel>();
            }
        }
    }

    /// <summary>
    /// キーUIを全て非表示にし、親をEnemySelectNumberに戻しておく
    /// </summary>
    private void ResetKeyUIs()
    {
        if (_keyUIInstances == null) return;

        foreach (var key in _keyUIInstances)
        {
            if (key != null)
            {
                key.SetActive(false);
                // 親設定がおかしくなっている場合に備えてリセット（必要に応じて）
                if (EnemySelectNumber != null)
                {
                    key.transform.SetParent(EnemySelectNumber.transform);
                }
            }
        }
    }

    /// <summary>
    /// UIプレハブをインスタンス化し、管理リストに登録する
    /// エラー対策：毎回Destroyするのではなく、なければ作るスタイルに変更
    /// </summary>
    private void SetupKeyUIInstances()
    {
        if (_keyUIInstances == null)
        {
            _keyUIInstances = new List<GameObject>();
        }

        // 既に数が足りているなら何もしない（エラー回避のため再生成しない）
        if (_keyUIInstances.Count >= 3 && _keyUIInstances.All(x => x != null))
        {
            return;
        }

        // リストの中身をクリアして作り直す（nullなどが混じっている場合のみ）
        foreach (var obj in _keyUIInstances)
        {
            if (obj != null) Destroy(obj);
        }
        _keyUIInstances.Clear();

        void CreateAndAdd(GameObject prefab)
        {
            if (prefab != null && EnemySelectNumber != null)
            {
                GameObject instance = Instantiate(prefab, EnemySelectNumber.transform);
                instance.SetActive(false);
                _keyUIInstances.Add(instance);
            }
        }

        CreateAndAdd(key1UI);
        CreateAndAdd(key2UI);
        CreateAndAdd(key3UI);
    }

    /// <summary>
    /// 保持している全てのプレイヤーの選択リストをクリアする
    /// </summary>
    public void ClearSelections()
    {
        if (PlayerSelections == null) return;

        foreach (var player in _currentPlayers)
        {
            if (PlayerSelections.ContainsKey(player.ID))
            {
                PlayerSelections[player.ID].Clear();
            }
        }
        Debug.Log("全プレイヤーのターゲット選択がクリアされました。");
    }

    /// <summary>
    /// 死んでいる敵へのターゲットを除外する
    /// "保持して続行" の際に、前のターンで倒した敵が含まれているとエラーになるため
    /// </summary>
    public void ValidateSelections()
    {
        if (PlayerSelections == null) return;

        foreach (var player in _currentPlayers)
        {
            if (PlayerSelections.ContainsKey(player.ID))
            {
                // HPが0以下の敵、またはリストから消滅している敵を除外
                PlayerSelections[player.ID].RemoveAll(enemy => enemy == null || enemy.EnemyHP <= 0);
            }
        }
    }

    public void StartPhase()
    {
        StartCoroutine(SelectionProcessCoroutine(isKeepingSelections));
        isKeepingSelections = false;
    }

    public void StartPhase(bool keepSelections)
    {
        isKeepingSelections = keepSelections;
        StartPhase();
    }

    private IEnumerator SelectionProcessCoroutine(bool keepSelections)
    {
        Debug.Log($"選択ターンのプレイヤー数: {_currentPlayers.Count}, 選択保持: {keepSelections}");

        for (int pIndex = 0; pIndex < _currentPlayers.Count; pIndex++)
        {
            PlayerRuntime currentPlayer = _currentPlayers[pIndex];
            if (pIndex >= _playerUIs.Count) continue;

            // 選択保持モードでない、または保持モードだがUI表示が必要な場合のみリセット
            if (!keepSelections)
            {
                ResetKeyUIs();
                _playerUIs[pIndex].SetHighlight(new Color(0.5f, 0.8f, 1f));
            }

            int currentLivingEnemies = _currentEnemies.Count(e => e.EnemyHP > 0);
            if (currentLivingEnemies == 0)
            {
                _playerUIs[pIndex].ResetHighlight();
                continue;
            }

            // 優先順位分の選択ループ
            for (int priority = 1; priority <= _currentPlayers.Count; priority++)
            {
                livingEnemyCount = _currentEnemies.Count(e => e.EnemyHP > 0);
                if (livingEnemyCount == 0) break;

                bool hasValidSelection = false;
                if (keepSelections && PlayerSelections.ContainsKey(currentPlayer.ID))
                {
                    if (PlayerSelections[currentPlayer.ID].Count >= priority)
                    {
                        var target = PlayerSelections[currentPlayer.ID][priority - 1];
                        if (target != null && target.EnemyHP > 0)
                        {
                            hasValidSelection = true;
                            // Debug.Log($"優先度{priority}: 既存のターゲット {target.EnemyName} を使用します。");
                        }
                    }
                }

                // 既に有効な選択があるなら入力コルーチンをスキップ
                if (hasValidSelection)
                {
                    continue;
                }

                // UIハイライト（プレイヤー）
                if (keepSelections)
                {
                    _playerUIs[pIndex].SetHighlight(new Color(0.5f, 0.8f, 1f));
                }

                yield return StartCoroutine(SelectOneTargetCoroutine(currentPlayer, priority, (selectedEnemy) =>
                {
                    // 選択時コールバック
                }, keepSelections));
            }

            _playerUIs[pIndex].ResetHighlight();

            if (livingEnemyCount == 0)
            {
                Debug.Log("全ての敵が倒されました。");
                break;
            }
        }

        FinishSelectTurn();
        OnPhaseFinished?.Invoke();
    }

    public IEnumerator SelectOneTargetCoroutine(PlayerRuntime player, int priority, System.Action<EnemyModel> onSelected, bool keepSelections = false)
    {
        var livingEnemies = _currentEnemies.Where(e => e.EnemyHP > 0).ToList();
        if (livingEnemies.Count == 0) yield break;

        currentTargetIndex = 0;
        previousTargetIndex = 0;

        // ターゲットカーソル（敵UI）のハイライト
        EnemyStatusUIController targetUI = _enemyUIs.FirstOrDefault(ui => ui.GetEnemyModel() == livingEnemies[currentTargetIndex]);
        if (targetUI != null)
        {
            targetUI.SetHighlight(new Color(1f, 0.5f, 0.5f));
        }

        // 入力イベント登録
        selectInputReader.UpStatusEvent += OnUpPressed;
        selectInputReader.DownStatusEvent += OnDownPressed;
        selectInputReader.ConfirmEvent += OnConfirmPressed;

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
                EnemyModel prevModel = livingEnemies[previousTargetIndex];
                EnemyStatusUIController prevUI = _enemyUIs.FirstOrDefault(ui => ui.GetEnemyModel() == prevModel);
                if (prevUI != null) prevUI.ResetHighlight();

                EnemyModel currentModel = livingEnemies[currentTargetIndex];
                EnemyStatusUIController currentUI = _enemyUIs.FirstOrDefault(ui => ui.GetEnemyModel() == currentModel);
                if (currentUI != null) currentUI.SetHighlight(new Color(1f, 0.5f, 0.5f));

                previousTargetIndex = currentTargetIndex;
            }

            if (confirmPressed)
            {
                confirmPressed = false;
                EnemyModel selectedEnemy = livingEnemies[currentTargetIndex];

                // 重複チェック
                if (PlayerSelections[player.ID].Contains(selectedEnemy))
                {
                    //Debug.Log("その敵は既に選択済みです。別の敵を選択してください。");
                    continue;
                }

                SoundManager.Instance.PlaySE(SEType.Check);
                PlayerSelections[player.ID].Add(selectedEnemy);
                onSelected?.Invoke(selectedEnemy);

                // 番号キーUIの表示
                int keyIndex = priority - 1;
                if (_keyUIInstances != null && keyIndex >= 0 && keyIndex < _keyUIInstances.Count)
                {
                    GameObject targetKeyUI = _keyUIInstances[keyIndex];
                    if (targetKeyUI != null && EnemySelectNumber != null)
                    {
                        targetKeyUI.transform.SetParent(EnemySelectNumber.transform);
                        targetKeyUI.transform.localScale = Vector3.one;
                        targetKeyUI.SetActive(true);
                    }
                }

                // 全敵UIのハイライト解除
                foreach (var eUI in _enemyUIs) eUI.ResetHighlight();

                selectionMade = true;
            }
        }

        selectInputReader.UpStatusEvent -= OnUpPressed;
        selectInputReader.DownStatusEvent -= OnDownPressed;
        selectInputReader.ConfirmEvent -= OnConfirmPressed;
    }

    public void FinalizeSelectionsForTutorial()
    {
        var livingEnemies = _currentEnemies.Where(e => e.EnemyHP > 0).ToList();
        if (livingEnemies.Count == 0) return;
        foreach (var player in _currentPlayers)
        {
            if (PlayerSelections.ContainsKey(player.ID) && PlayerSelections[player.ID].Count == 0)
            {
                PlayerSelections[player.ID].Add(livingEnemies[0]);
            }
        }
    }

    private void FinishSelectTurn()
    {
        SelectTurnFinished?.Invoke();
    }
}