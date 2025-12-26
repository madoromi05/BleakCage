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
            if (key != null)
            {
                key.SetActive(false);
                // ★修正: 待避先をこのオブジェクト自身に変更
                key.transform.SetParent(this.transform);
            }
        }
    }

    private void SetupKeyUIInstances()
    {
        if (_keyUIInstances == null)
        {
            _keyUIInstances = new List<GameObject>();
        }

        if (_keyUIInstances.Count >= 3 && _keyUIInstances.All(x => x != null))
        {
            return;
        }

        foreach (var obj in _keyUIInstances)
        {
            if (obj != null) Destroy(obj);
        }
        _keyUIInstances.Clear();

        void CreateAndAdd(GameObject prefab)
        {
            if (prefab != null)
            {
                // ★修正: 親をこのオブジェクトにして生成
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
        if (PlayerSelections == null) return;

        foreach (var player in _currentPlayers)
        {
            if (PlayerSelections.ContainsKey(player.ID))
            {
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

            for (int priority = 1; priority <= _currentPlayers.Count; priority++)
            {
                livingEnemyCount = _currentEnemies.Count(e => e.EnemyHP > 0);
                if (livingEnemyCount == 0) break;
                int currentSelectedCount = 0;
                if (PlayerSelections.ContainsKey(currentPlayer.ID))
                {
                    currentSelectedCount = PlayerSelections[currentPlayer.ID].Count;
                }

                if (currentSelectedCount >= livingEnemyCount)
                {
                    Debug.Log("これ以上選べる敵がいません（全敵選択済み）。次のプレイヤーへ進みます。");
                    break;
                }
                bool hasValidSelection = false;
                if (keepSelections && PlayerSelections.ContainsKey(currentPlayer.ID))
                {
                    if (PlayerSelections[currentPlayer.ID].Count >= priority)
                    {
                        var target = PlayerSelections[currentPlayer.ID][priority - 1];
                        if (target != null && target.EnemyHP > 0)
                        {
                            hasValidSelection = true;
                            // ★修正: 選択保持時、敵の頭上にキーを表示
                            ShowKeyOnEnemy(priority, target);
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

        EnemyStatusUIController targetUI = _enemyUIs.FirstOrDefault(ui => ui.GetEnemyModel() == livingEnemies[currentTargetIndex]);
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

                if (PlayerSelections[player.ID].Contains(selectedEnemy))
                {
                    continue;
                }

                SoundManager.Instance.PlaySE(SEType.Check);
                PlayerSelections[player.ID].Add(selectedEnemy);
                onSelected?.Invoke(selectedEnemy);
                ShowKeyOnEnemy(priority, selectedEnemy);

                foreach (var eUI in _enemyUIs) eUI.ResetHighlight();

                // ここで確実にtrueにする（遷移の鍵）
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

    private void ShowKeyOnEnemy(int priority, EnemyModel targetEnemy)
    {
        int keyIndex = priority - 1;

        if (_keyUIInstances == null || keyIndex < 0 || keyIndex >= _keyUIInstances.Count) return;

        GameObject targetKeyUI = _keyUIInstances[keyIndex];
        if (targetKeyUI == null) return;

        EnemyStatusUIController targetEnemyUI = _enemyUIs.FirstOrDefault(ui => ui.GetEnemyModel() == targetEnemy);

        if (targetEnemyUI != null)
        {
            Transform anchor = targetEnemyUI.GetKeyUiAnchor();

            targetKeyUI.transform.SetParent(anchor);
            targetKeyUI.transform.localPosition = Vector3.zero;
            targetKeyUI.transform.localScale = Vector3.one;
            targetKeyUI.SetActive(true);
        }
    }
}