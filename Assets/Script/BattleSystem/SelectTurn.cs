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

    public Dictionary<int, List<EnemyModel>> PlayerSelections { get; private set; }

    public event System.Action SelectTurnFinished;
    public event System.Action OnPhaseFinished;

    private void OnUpPressed() { upPressed = true; }
    private void OnDownPressed() { downPressed = true; }
    private void OnConfirmPressed() { confirmPressed = true; }

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

    public void Initialize(List<PlayerRuntime> players, List<EnemyModel> enemies,
    List<PlayerStatusUIController> pUIs, List<EnemyStatusUIController> eUIs)
    {
        _currentPlayers = players.Where(p => p != null && p.CurrentHP > 0).ToList();
        _currentEnemies = enemies.Where(e => e != null && e.EnemyHP > 0).ToList();
        _playerUIs = pUIs;
        _enemyUIs = eUIs.Where(ui => ui != null).ToList();
        SetupKeyUIInstances();
        ResetKeyUIs();

        if (PlayerSelections == null)
        {
            PlayerSelections = new Dictionary<int, List<EnemyModel>>();
        }

        foreach (var player in _currentPlayers)
        {
            if (!PlayerSelections.ContainsKey(player.ID))
            {
                PlayerSelections[player.ID] = new List<EnemyModel>();
            }
        }
    }

    private void ResetKeyUIs()
    {
        if (_keyUIInstances == null) return;

        foreach (var key in _keyUIInstances)
        {
            //Key自体がDestroyされている可能性があるためnullチェック
            if (key != null)
            {
                key.SetActive(false);
                key.transform.SetParent(this.transform);
                key.transform.localScale = Vector3.one;
            }
        }
    }
    private void SetupKeyUIInstances()
    {
        if (_keyUIInstances == null)
        {
            _keyUIInstances = new List<GameObject>();
        }

        // 全て存在しているなら何もしない
        if (_keyUIInstances.Count >= 3 && _keyUIInstances.All(x => x != null))
        {
            return;
        }

        // 欠損がある場合は作り直す
        foreach (var obj in _keyUIInstances)
        {
            if (obj != null) Destroy(obj);
        }
        _keyUIInstances.Clear();

        void CreateAndAdd(GameObject prefab)
        {
            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab, this.transform);
                instance.SetActive(false);
                _keyUIInstances.Add(instance);
            }
        }

        CreateAndAdd(key1UI);
        CreateAndAdd(key2UI);
        CreateAndAdd(key3UI);
    }

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

    public void ValidateSelections()
    {
        if (_currentEnemies != null)
        {
            _currentEnemies.RemoveAll(e => e == null || e.EnemyHP <= 0);
        }

        if (PlayerSelections != null)
        {
            foreach (var player in _currentPlayers)
            {
                if (PlayerSelections.ContainsKey(player.ID))
                {
                    PlayerSelections[player.ID].RemoveAll(enemy => enemy == null || enemy.EnemyHP <= 0);
                }
            }
        }

        // ★追加: UIリストからも死んだ敵のUIを掃除しておく
        if (_enemyUIs != null)
        {
            _enemyUIs.RemoveAll(ui => ui == null);
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
        ValidateSelections();

        for (int pIndex = 0; pIndex < _currentPlayers.Count; pIndex++)
        {
            PlayerRuntime currentPlayer = _currentPlayers[pIndex];
            if (currentPlayer == null || currentPlayer.CurrentHP <= 0) continue;

            if (!keepSelections)
            {
                ResetKeyUIs();
                _playerUIs[pIndex].SetHighlight(new Color(0.5f, 0.8f, 1f));
            }

            if (_currentEnemies.Count == 0)
            {
                _playerUIs[pIndex].ResetHighlight();
                break;
            }

            int livingCount = _currentPlayers.Count(p => p != null && p.CurrentHP > 0);
            for (int priority = 1; priority <= livingCount; priority++)
            {
                if (_currentEnemies.Count == 0) break;

                int currentSelectedCount = 0;
                if (PlayerSelections.ContainsKey(currentPlayer.ID))
                {
                    currentSelectedCount = PlayerSelections[currentPlayer.ID].Count;
                }

                if (currentSelectedCount >= _currentEnemies.Count)
                {
                    Debug.Log($"これ以上選べる敵がいません（全敵選択済み）。次のプレイヤーへ進みます。");
                    break;
                }

                bool hasValidSelection = false;
                if (keepSelections && PlayerSelections.ContainsKey(currentPlayer.ID))
                {
                    if (PlayerSelections[currentPlayer.ID].Count >= priority)
                    {
                        var target = PlayerSelections[currentPlayer.ID][priority - 1];
                        if (target != null && _currentEnemies.Contains(target))
                        {
                            hasValidSelection = true;
                            ShowKeyOnEnemy(priority, target);
                        }
                        else
                        {
                            PlayerSelections[currentPlayer.ID].RemoveAt(priority - 1);
                            hasValidSelection = false;
                        }
                    }
                }

                if (hasValidSelection)
                {
                    continue;
                }

                if (keepSelections)
                {
                    _playerUIs[pIndex].SetHighlight(new Color(0.5f, 0.8f, 1f));
                }

                yield return StartCoroutine(SelectOneTargetCoroutine(currentPlayer, priority, (selectedEnemy) =>
                { }, keepSelections));
            }

            _playerUIs[pIndex].ResetHighlight();

            if (_currentEnemies.Count == 0)
            {
                break;
            }
        }

        FinishSelectTurn();
        OnPhaseFinished?.Invoke();
    }

    public IEnumerator SelectOneTargetCoroutine(PlayerRuntime player, int priority, System.Action<EnemyModel> onSelected, bool keepSelections = false)
    {
        if (_currentEnemies.Count == 0) yield break;

        currentTargetIndex = 0;
        previousTargetIndex = 0;

        EnemyStatusUIController targetUI = _enemyUIs.FirstOrDefault(ui => ui != null && ui.GetEnemyModel() == _currentEnemies[currentTargetIndex]);
        if (targetUI != null)
        {
            targetUI.SetHighlight(new Color(1f, 0.5f, 0.5f));
        }

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

            if (_currentEnemies.Count == 0)
            {
                selectionMade = true;
                break;
            }

            bool selectionChanged = false;
            if (downPressed)
            {
                currentTargetIndex = (currentTargetIndex + 1) % _currentEnemies.Count;
                selectionChanged = true;
                downPressed = false;
            }
            else if (upPressed)
            {
                currentTargetIndex = (currentTargetIndex - 1 + _currentEnemies.Count) % _currentEnemies.Count;
                selectionChanged = true;
                upPressed = false;
            }

            if (selectionChanged)
            {
                if (previousTargetIndex < _currentEnemies.Count)
                {
                    EnemyModel prevModel = _currentEnemies[previousTargetIndex];
                    EnemyStatusUIController prevUI = _enemyUIs.FirstOrDefault(ui => ui != null && ui.GetEnemyModel() == prevModel);
                    if (prevUI != null) prevUI.ResetHighlight();
                }
                else
                {
                    // nullチェックを追加
                    foreach (var ui in _enemyUIs) if (ui != null) ui.ResetHighlight();
                }

                EnemyModel currentModel = _currentEnemies[currentTargetIndex];
                EnemyStatusUIController currentUI = _enemyUIs.FirstOrDefault(ui => ui != null && ui.GetEnemyModel() == currentModel);
                if (currentUI != null) currentUI.SetHighlight(new Color(1f, 0.5f, 0.5f));

                previousTargetIndex = currentTargetIndex;
            }

            if (confirmPressed)
            {
                confirmPressed = false;
                EnemyModel selectedEnemy = _currentEnemies[currentTargetIndex];

                if (PlayerSelections[player.ID].Contains(selectedEnemy))
                {
                    continue;
                }

                SoundManager.Instance.PlaySE(SEType.Check);
                PlayerSelections[player.ID].Add(selectedEnemy);
                onSelected?.Invoke(selectedEnemy);
                ShowKeyOnEnemy(priority, selectedEnemy);

                foreach (var eUI in _enemyUIs) if (eUI != null) eUI.ResetHighlight();

                selectionMade = true;
            }
        }

        selectInputReader.UpStatusEvent -= OnUpPressed;
        selectInputReader.DownStatusEvent -= OnDownPressed;
        selectInputReader.ConfirmEvent -= OnConfirmPressed;
    }

    public void FinalizeSelectionsForTutorial()
    {
        var livingEnemies = _currentEnemies.Where(e => e != null && e.EnemyHP > 0).ToList();
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
        ResetKeyUIs();
        SelectTurnFinished?.Invoke();
    }

    private void ShowKeyOnEnemy(int priority, EnemyModel targetEnemy)
    {
        int keyIndex = priority - 1;
        SetupKeyUIInstances();

        if (_keyUIInstances == null || keyIndex < 0 || keyIndex >= _keyUIInstances.Count) return;
        if (_keyUIInstances[keyIndex] == null)
        {
            SetupKeyUIInstances();
        }

        GameObject targetKeyUI = _keyUIInstances[keyIndex];
        if (targetKeyUI == null) return;

        EnemyStatusUIController targetEnemyUI = _enemyUIs.FirstOrDefault(ui => ui != null && ui.GetEnemyModel() == targetEnemy);

        if (targetEnemyUI != null)
        {
            Transform anchor = targetEnemyUI.GetKeyUiAnchor();

            targetKeyUI.transform.SetParent(anchor);
            targetKeyUI.transform.localPosition = Vector3.zero;
            targetKeyUI.transform.localScale = Vector3.one;
            targetKeyUI.SetActive(true);
        }
    }

    /// <summary>
    /// 指定された敵が死亡した場合、選択リストから完全に削除し、
    /// UI（キー表示）も更新する
    /// </summary>
    public void RemoveEnemyFromSelections(EnemyModel deadEnemy)
    {
        // 1. 敵リストから削除
        if (_currentEnemies != null)
        {
            _currentEnemies.Remove(deadEnemy);
        }

        //
        // Iリストからも削除（nullまたは対象のUI）
        if (_enemyUIs != null)
        {
            _enemyUIs.RemoveAll(ui => ui == null || ui.GetEnemyModel() == deadEnemy);
        }

        // 3. 選択済みリストから削除
        if (PlayerSelections == null) return;

        foreach (var playerID in PlayerSelections.Keys.ToList())
        {
            var selectionList = PlayerSelections[playerID];
            if (selectionList.Contains(deadEnemy))
            {
                selectionList.RemoveAll(e => e == deadEnemy);

                SetupKeyUIInstances();
            }
        }
    }

    public void RemovePlayerFromSelections(PlayerRuntime deadPlayer)
    {
        if (deadPlayer == null) return;

        if (PlayerSelections != null && PlayerSelections.ContainsKey(deadPlayer.ID))
        {
            PlayerSelections.Remove(deadPlayer.ID);
        }
    }

}