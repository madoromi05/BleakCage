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

    // PlayerID -> UI の辞書（UIは辞書参照に統一）
    private Dictionary<int, PlayerStatusUIController> _playerUiById;

    public event System.Action SelectTurnFinished;
    public event System.Action OnPhaseFinished;

    private void OnUpPressed() { upPressed = true; }
    private void OnDownPressed() { downPressed = true; }
    private void OnConfirmPressed() { confirmPressed = true; }

    private int currentTargetIndex;
    private int previousTargetIndex;

    private List<PlayerRuntime> _currentPlayers;
    private List<EnemyModel> _currentEnemies;

    private List<EnemyStatusUIController> _enemyUIs;
    private List<GameObject> _keyUIInstances;

    private bool upPressed = false;
    private bool downPressed = false;
    private bool confirmPressed = false;
    private bool isKeepingSelections = false;

    public void Initialize(
        List<PlayerRuntime> players,
        List<EnemyModel> enemies,
        List<PlayerStatusUIController> pUIs,
        List<EnemyStatusUIController> eUIs)
    {
        // 生存者だけ優先順位選択
        _currentPlayers = (players ?? new List<PlayerRuntime>())
            .Where(p => p != null && p.CurrentHP > 0).ToList();

        _currentEnemies = (enemies ?? new List<EnemyModel>())
            .Where(e => e != null && e.EnemyHP > 0).ToList();

        _playerUiById = new Dictionary<int, PlayerStatusUIController>();
        if (pUIs != null)
        {
            foreach (var ui in pUIs)
            {
                if (ui == null) continue;
                var rt = ui.GetPlayerRuntime();
                if (rt == null) continue;
                _playerUiById[rt.ID] = ui;
            }
        }

        // 敵UI
        _enemyUIs = (eUIs ?? new List<EnemyStatusUIController>())
            .Where(ui => ui != null).ToList();

        SetupKeyUIInstances();
        ResetKeyUIs();

        // PlayerSelections の整合（死んだプレイヤーIDは掃除）
        if (PlayerSelections == null) PlayerSelections = new Dictionary<int, List<EnemyModel>>();
        var aliveIds = new HashSet<int>(_currentPlayers.Select(p => p.ID));

        foreach (var key in PlayerSelections.Keys.ToList())
        {
            if (!aliveIds.Contains(key)) PlayerSelections.Remove(key);
        }

        foreach (var player in _currentPlayers)
        {
            if (!PlayerSelections.ContainsKey(player.ID))
                PlayerSelections[player.ID] = new List<EnemyModel>();
        }
    }

    private void ResetKeyUIs()
    {
        if (_keyUIInstances == null) return;

        foreach (var key in _keyUIInstances)
        {
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
        if (_keyUIInstances == null) _keyUIInstances = new List<GameObject>();

        if (_keyUIInstances.Count >= 3 && _keyUIInstances.All(x => x != null))
            return;

        foreach (var obj in _keyUIInstances)
        {
            if (obj != null) Destroy(obj);
        }
        _keyUIInstances.Clear();

        void CreateAndAdd(GameObject prefab)
        {
            if (prefab == null) return;
            GameObject instance = Instantiate(prefab, this.transform);
            instance.SetActive(false);
            _keyUIInstances.Add(instance);
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
        DebugCostom.Log("全プレイヤーのターゲット選択がクリアされました。");
    }

    public void ValidateSelections()
    {
        // ★敵の生存整合
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

        // プレイヤーごとの選択
        for (int pIndex = 0; pIndex < _currentPlayers.Count; pIndex++)
        {
            PlayerRuntime currentPlayer = _currentPlayers[pIndex];
            if (currentPlayer == null || currentPlayer.CurrentHP <= 0) continue;

            // UIは辞書参照
            _playerUiById.TryGetValue(currentPlayer.ID, out var pui);

            if (!keepSelections)
            {
                ResetKeyUIs();
                if (pui != null) pui.SetHighlight(new Color(0.5f, 0.8f, 1f));
            }

            if (_currentEnemies.Count == 0)
            {
                if (pui != null) pui.ResetHighlight();
                break;
            }

            // 優先順位は「敵の生存数まで」
            for (int priority = 1; ; priority++)
            {
                ValidateSelections(); // ターン中に死んだ場合の安全策

                int aliveEnemyCount = _currentEnemies.Count; // Validate後なので Count でOK
                if (aliveEnemyCount <= 0) break;
                if (priority > aliveEnemyCount) break;

                int currentSelectedCount = 0;
                if (PlayerSelections.ContainsKey(currentPlayer.ID))
                    currentSelectedCount = PlayerSelections[currentPlayer.ID].Count;

                if (currentSelectedCount >= aliveEnemyCount) { break; }

                bool hasValidSelection = false;

                // keepSelections：既に選択済みがあるならそれを表示して次へ
                if (keepSelections && PlayerSelections.ContainsKey(currentPlayer.ID))
                {
                    if (PlayerSelections[currentPlayer.ID].Count >= priority)
                    {
                        var target = PlayerSelections[currentPlayer.ID][priority - 1];
                        if (target != null && _currentEnemies.Contains(target) && target.EnemyHP > 0)
                        {
                            hasValidSelection = true;
                            ShowKeyOnEnemy(priority, target);
                        }
                        else
                        {
                            // 壊れてる要素は除去して選び直し
                            PlayerSelections[currentPlayer.ID].RemoveAt(priority - 1);
                            hasValidSelection = false;
                        }
                    }
                }

                if (hasValidSelection) continue;

                if (keepSelections)
                {
                    if (pui != null) pui.SetHighlight(new Color(0.5f, 0.8f, 1f));
                }

                yield return StartCoroutine(
                    SelectOneTargetCoroutine(currentPlayer, priority, (selectedEnemy) => { }, keepSelections)
                );
            }

            if (pui != null) pui.ResetHighlight();

            if (_currentEnemies.Count == 0) break;
        }

        FinishSelectTurn();
        OnPhaseFinished?.Invoke();
    }

    public IEnumerator SelectOneTargetCoroutine(
        PlayerRuntime player,
        int priority,
        System.Action<EnemyModel> onSelected,
        bool keepSelections = false)
    {
        if (_currentEnemies.Count == 0) yield break;

        currentTargetIndex = 0;
        previousTargetIndex = 0;

        EnemyStatusUIController targetUI = _enemyUIs.FirstOrDefault(
            ui => ui != null && ui.GetEnemyModel() == _currentEnemies[currentTargetIndex]
        );
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

            ValidateSelections();
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
                if (selectedEnemy == null) continue;

                if (!PlayerSelections.ContainsKey(player.ID))
                {
                    PlayerSelections[player.ID] = new List<EnemyModel>();
                }

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

    public void RemoveEnemyFromSelections(EnemyModel deadEnemy)
    {
        if (_currentEnemies != null) _currentEnemies.Remove(deadEnemy);

        if (_enemyUIs != null)
        {
            _enemyUIs.RemoveAll(ui => ui == null || ui.GetEnemyModel() == deadEnemy);
        }

        if (PlayerSelections == null) return;

        foreach (var playerID in PlayerSelections.Keys.ToList())
        {
            PlayerSelections[playerID].RemoveAll(e => e == null || e == deadEnemy);
        }

        ResetKeyUIs();
        // すぐ表示を再構築したいなら、ここで「既存選択をもとに ShowKeyOnEnemy を呼び直す」設計にすると綺麗
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
