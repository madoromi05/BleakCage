using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 敵ターンの処理 (攻撃のキューイングと実行、防御判定の委譲)
///</summary>
public class EnemyTurn : MonoBehaviour
{
    public event System.Action TurnFinished;

    [Header("Component References")]
    [SerializeField] private BattleManager _battleManager;
    [SerializeField] private BattleEntitiesManager _entitiesManager;
    [SerializeField] private PlayerDefenseHandler _defenseHandler;

    private List<PlayerRuntime> _players;
    private List<EnemyModel> _enemies;
    private List<EnemyRuntime> _enemyRuntimes;

    private readonly Queue<ICommand> _commandQueue = new Queue<ICommand>();

    private List<PlayerStatusUIController> _playerStatusUiControllers;
    private Dictionary<EnemyModel, EnemyController> _enemyControllers;
    private Dictionary<PlayerRuntime, PlayerController> _playerControllers;

    private PlayerRuntime _currentPlayerTarget;
    private EnemyAttackCommand _currentAttackCommand;
    private bool _attackHasBeenResolved;

    private void Awake()
    {
        if (_defenseHandler != null && _battleManager != null)
        {
            _defenseHandler.OnDefenseResultFeedback += _battleManager.ShowDefenseFeedback;
            _defenseHandler.OnDamageToPlayer += HandleDamageToPlayer;
        }
    }

    public void EnemySetup(
        List<PlayerRuntime> players,
        List<EnemyModel> enemies,
        Dictionary<EnemyModel, EnemyController> enemyControllers,
        Dictionary<PlayerRuntime, PlayerController> playerControllers,
        List<PlayerStatusUIController> playerStatusUiControllers,
        List<EnemyRuntime> runtimeList)
    {
        _players = players;
        _enemies = enemies;
        _enemyControllers = enemyControllers;
        _playerControllers = playerControllers;
        _playerStatusUiControllers = playerStatusUiControllers;
        _enemyRuntimes = runtimeList;

        _defenseHandler.Init(players, playerControllers);

        foreach (EnemyController enemyController in _enemyControllers.Values)
        {
            if (enemyController == null) continue;

            enemyController.OnAttackHitMoment += () =>
            {
                if (_defenseHandler == null) return;
                StartCoroutine(_defenseHandler.StartDefenseWindowCoroutine(_currentPlayerTarget, ResolveAttack));
            };
        }
    }

    public void StartEnemyTurn()
    {
        _commandQueue.Clear();
        _defenseHandler.EnableDefenseInput();
        StartCoroutine(ProcessEnemyActions());
    }
    /// <summary>
    /// 攻撃対象となる生存プレイヤーをランダムに選択する
    /// </summary>
    private PlayerRuntime GetRandomLivingPlayer()
    {
        List<PlayerRuntime> livingPlayers = _players.Where(p => p != null && p.CurrentHP > 0).ToList();
        if (livingPlayers.Count <= 0) return null;

        int choice = Random.Range(0, livingPlayers.Count);
        return livingPlayers[choice];
    }

    private void PrepareAttackCommands()
    {
        if (_enemyRuntimes == null) return;

        foreach (EnemyRuntime attackerRuntime in _enemyRuntimes)
        {
            if (attackerRuntime == null || attackerRuntime.CurrentHP <= 0) continue;

            PlayerRuntime targetRuntime = GetRandomLivingPlayer();
            if (targetRuntime == null) continue;

            EnemyModel attackerModel = attackerRuntime.EnemyModel;

            PlayerStatusUIController targetUiController = _playerStatusUiControllers
                .FirstOrDefault(ui => ui != null && ui.GetPlayerRuntime() == targetRuntime);

            _enemyControllers.TryGetValue(attackerModel, out EnemyController attackerController);
            _playerControllers.TryGetValue(targetRuntime, out PlayerController targetController);

            _commandQueue.Enqueue(new EnemyAttackCommand(
                targetRuntime,
                attackerModel,
                attackerController,
                targetController,
                targetUiController
            ));
        }
    }

    /// <summary>
    /// 防御判定が完了したときに DefenseHandler から呼ばれる
    /// </summary>
    private void ResolveAttack()
    {
        _attackHasBeenResolved = true;
    }


    /// <summary>
    /// 敵の行動を順次実行するコルーチン
    /// </summary>
    private IEnumerator ProcessEnemyActions()
    {
        PrepareAttackCommands();

        while (_commandQueue.Count > 0)
        {
            ICommand command = _commandQueue.Dequeue();
            EnemyAttackCommand attackCmd = command as EnemyAttackCommand;
            if (attackCmd == null)
            {
                yield return new WaitForSeconds(0.3f);
                continue;
            }

            // この攻撃のコンテキストをセット（共通）
            _currentPlayerTarget = attackCmd.PlayerTarget;
            _currentAttackCommand = attackCmd;
            _attackHasBeenResolved = false;

            int targetPlayerIndex = _players.FindIndex(p => p == _currentPlayerTarget);

            if (targetPlayerIndex != -1 && _entitiesManager != null && _battleManager != null)
            {
                _entitiesManager.ShowTargetMarkerOnPlayer(_battleManager.MarkerInstance, targetPlayerIndex);
            }

            yield return new WaitForSeconds(0.5f);

            yield return StartCoroutine(command.Do());

            if (_entitiesManager != null && _battleManager != null)
            {
                _entitiesManager.HideTargetMarker(_battleManager.MarkerInstance);
            }

            if (!_attackHasBeenResolved)
            {
                StartCoroutine(_defenseHandler.StartDefenseWindowCoroutine(_currentPlayerTarget, ResolveAttack));
            }

            yield return new WaitUntil(() => _attackHasBeenResolved);
            yield return null;
        }

        yield return new WaitForSeconds(1.0f);
        _defenseHandler.DisableDefenseInput();
        TurnFinished?.Invoke();
    }


    /// <summary>
    /// PlayerDefenseHandler からダメージ発生時に呼ばれる
    /// </summary>
    private void HandleDamageToPlayer(PlayerRuntime target)
    {
        _currentAttackCommand?.ApplyDamageAfterJudgement();
    }

    private void OnDestroy()
    {
        if (_defenseHandler != null && _battleManager != null)
        {
            _defenseHandler.OnDefenseResultFeedback -= _battleManager.ShowDefenseFeedback;
            _defenseHandler.OnDamageToPlayer -= HandleDamageToPlayer;
        }
    }
}