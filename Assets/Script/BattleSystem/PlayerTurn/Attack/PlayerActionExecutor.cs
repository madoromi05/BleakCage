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
        Dictionary<EnemyModel, EnemyController> enemyControllers,
        IAttackStrategy damageStrategy,
        System.Action onExecutionComplete)
    {
        Debug.Log("実行するカード: " + string.Join(",", selectedCards.Select(c => c.ID)));
        commandQueue.Clear();

        foreach (var selectedCardRuntime in selectedCards)
        {
            // 1. このカードがアタッチされている特定の武器を取得する
            WeaponRuntime weaponRuntime = selectedCardRuntime.weaponRuntime;

            // 2. その武器を所持しているプレイヤーを取得する
            PlayerRuntime player = weaponRuntime.ParentPlayer;
            if (player == null || weaponRuntime == null)
            {
                Debug.LogError($"カード {selectedCardRuntime.ID} ({selectedCardRuntime.InstanceID}) はどの武器にもアタッチされていません！");
                Debug.LogError($"武器 {weaponRuntime.ID} ({weaponRuntime.InstanceID}) はどのプレイヤーにも所持されていません！");
                continue;
            }

            // 3. カードの属性に応じて処理を振り分け
            AttributeType attribute = selectedCardRuntime.attribute;

            if (IsAttackAttribute(attribute))
            {
                // 攻撃属性の場合
                if (playerTargetSelections.TryGetValue(player.ID, out List<EnemyModel> targets))
                {
                    HandleAttackAction(player, weaponRuntime, selectedCardRuntime, targets, enemyStatusUIControllers, enemyControllers, damageStrategy);
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
        Dictionary<EnemyModel, EnemyController> enemyControllers,
        IAttackStrategy damageStrategy)
    {
        EnemyModel finalTarget = null;
        foreach (var potentialTarget in targets)
        {
            if (potentialTarget.EnemyHP > 0)
            {
                finalTarget = potentialTarget;
                break;
            }
        }

        if (finalTarget != null)
        {
            EnemyStatusUIController targetEnemyUI = enemyStatusUIControllers.FirstOrDefault(ui => ui.GetEnemyModel() == finalTarget);
            if (targetEnemyUI != null && enemyControllers.TryGetValue(finalTarget, out EnemyController targetEnemyController))
            {
                Transform targetTransform = targetEnemyController.transform;
                commandQueue.Enqueue(new AttackCommand(attackPlayer, weaponRuntime, selectedCardRuntime,
                                                      targetEnemyUI, finalTarget,
                                                      targetTransform,
                                                      damageStrategy, cardModelFactory));
            }
            else
            {
                Debug.LogError($"攻撃対象 (ID: {finalTarget.EnemyID}) の EnemyController または UI が見つかりません。");
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