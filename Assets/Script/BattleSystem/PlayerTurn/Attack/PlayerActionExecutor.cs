/// <summary>
/// プレイヤーが選択したカードのアクション（攻撃など）を実行するクラス
/// </summary>
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerActionExecutor
{
    private Queue<ICommand> commandQueue = new();
    private CardModelFactory cardModelFactory;
    private MonoBehaviour coroutineRunner;

    public PlayerActionExecutor(MonoBehaviour runner)
    {
        this.cardModelFactory = new CardModelFactory();
        this.coroutineRunner = runner;
    }
    /// <summary>
    /// 選択されたカードのコマンドを生成し、実行するコルーチン
    /// </summary>
    public IEnumerator ExecuteActions(
        List<CardRuntime> selectedCards,
        Dictionary<int, List<EnemyModel>> playerTargetSelections,
        List<EnemyStatusUIController> enemyStatusUIControllers,
        List<PlayerStatusUIController> playerStatusUIControllers,
        Dictionary<EnemyModel, EnemyController> enemyControllers,
        List<EnemyRuntime> allEnemyRuntimes,
        DamageCalculator damageCalculator,
        System.Action onExecutionComplete)
    {
        Debug.Log("実行するカード: " + string.Join(",", selectedCards.Select(c => c.ID)));
        commandQueue.Clear();

        foreach (var selectedCardRuntime in selectedCards)
        {
            // このカードがアタッチされている特定の武器を取得する
            WeaponRuntime weaponRuntime = selectedCardRuntime.weaponRuntime;

            // その武器を所持しているプレイヤーを取得する
            PlayerRuntime player = weaponRuntime.ParentPlayer;
            if (player == null || weaponRuntime == null)
            {
                Debug.LogError($"カード {selectedCardRuntime.ID} ({selectedCardRuntime.InstanceID}) はどの武器にもアタッチされていません！");
                Debug.LogError($"武器 {weaponRuntime.ID} ({weaponRuntime.InstanceID}) はどのプレイヤーにも所持されていません！");
                continue;
            }

            // カードの属性に応じて処理を振り分け
            AttributeType attribute = selectedCardRuntime.attribute;

            if (IsAttackAttribute(attribute))
            {
                // 攻撃属性の場合
                if (playerTargetSelections.TryGetValue(player.ID, out List<EnemyModel> targets))
                {
                    HandleAttackAction(player, weaponRuntime, selectedCardRuntime, targets,
                         enemyStatusUIControllers, playerStatusUIControllers,
                         enemyControllers, allEnemyRuntimes, damageCalculator);
                }
            }
            else
            {
                HandleSupportAction(player, selectedCardRuntime);
            }
        }

        // 順に実行
        while (commandQueue.Count > 0)
        {
            var command = commandQueue.Dequeue();
            yield return coroutineRunner.StartCoroutine(command.Do());
        }

        Debug.Log("カード効果の実行完了");
        onExecutionComplete?.Invoke();
    }

    /// <summary>
    /// 渡された属性が攻撃属性（斬、鈍、突、弾）かどうかを判定する
    /// </summary>
    private bool IsAttackAttribute(AttributeType attribute)
    {
        switch (attribute)
        {
            case AttributeType.Slash:
            case AttributeType.Blunt:
            case AttributeType.Pierce:
            case AttributeType.Bullet:
                return true;
            case AttributeType.Heal:
            case AttributeType.Defence:
            default:
                return false;
        }
    }

    /// <summary>
    /// 攻撃属性カードの処理（コマンドをキューに追加）
    /// </summary>
    private void HandleAttackAction(
        PlayerRuntime attackPlayer,
        WeaponRuntime weaponRuntime,
        CardRuntime selectedCardRuntime,
        List<EnemyModel> targets,
        List<EnemyStatusUIController> enemyStatusUIControllers,
        List<PlayerStatusUIController> playerStatusUIControllers,
        Dictionary<EnemyModel, EnemyController> enemyControllers,
        List<EnemyRuntime> allEnemyRuntimes,
        DamageCalculator damageCalculator
    )
    {
        PlayerStatusUIController attackerUI = playerStatusUIControllers.FirstOrDefault(ui => ui.GetPlayerRuntime() == attackPlayer);

        // 優先順位リストに基づいて攻撃対象を決定
        EnemyRuntime finalTargetRuntime = null;
        EnemyModel finalTargetModel = null;

        foreach (var potentialTargetModel in targets)
        {
            // Modelに対応するRuntimeを検索する
            var runtime = allEnemyRuntimes.FirstOrDefault(r => r.EnemyModel == potentialTargetModel);

            if (runtime != null && runtime.CurrentHP > 0)
            {
                finalTargetRuntime = runtime;
                finalTargetModel = potentialTargetModel;
                break; // 生きている敵が見つかったので確定
            }
        }

        if (finalTargetRuntime != null)
        {
            EnemyStatusUIController targetEnemyUI = enemyStatusUIControllers.FirstOrDefault(ui => ui.GetEnemyModel() == finalTargetModel);
            if (targetEnemyUI != null && enemyControllers.TryGetValue(finalTargetModel, out EnemyController targetEnemyController))
            {
                Transform targetTransform = targetEnemyController.transform;
                commandQueue.Enqueue(new AttackCommand(
                attackPlayer,
                weaponRuntime,
                selectedCardRuntime,
                targetEnemyUI,
                attackerUI,
                finalTargetRuntime,
                targetTransform,
                damageCalculator,
                cardModelFactory));
            }
            else
            {
                Debug.LogError($"攻撃対象 (ID: {finalTargetRuntime.ID}) の EnemyController または UI が見つかりません。");
            }
        }
        else
        {
            Debug.LogWarning($"プレイヤー {attackPlayer.PlayerModel.PlayerName} の攻撃対象 (優先順位リスト) は全員倒されています。攻撃をスキップします。");
        }
    }

    /// <summary>
    /// 援属性カードの処理（コマンドをキューに追加）
    /// </summary>
    private void HandleSupportAction(PlayerRuntime player, CardRuntime selectedCardRuntime)
    {
        if (selectedCardRuntime.attribute == AttributeType.Heal)
        {
            // --- 回復コマンドの処理 ---
            Debug.Log($"{player.PlayerModel.PlayerName} が回復カードを使用。");
            SoundManager.Instance.PlaySE(SEType.Heal);
            // commandQueue.Enqueue(new HealCommand(player, 0.2f)); // 例：最大HPの20%回復
        }
        else if (selectedCardRuntime.attribute == AttributeType.Defence)
        {
            // --- 防御コマンドの処理 ---
            Debug.Log($"{player.PlayerModel.PlayerName} が防御カードを使用。");
            SoundManager.Instance.PlaySE(SEType.Defence);
            // commandQueue.Enqueue(new DefenceCommand(player, ...)); // 将来的な実装
        }
    }
}