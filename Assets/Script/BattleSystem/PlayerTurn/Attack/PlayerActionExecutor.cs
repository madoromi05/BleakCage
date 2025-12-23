/// <summary>
/// プレイヤーが選択したカードのアクション（攻撃など）を実行するクラス
/// </summary>
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerActionExecutor
{
    private Queue<(ICommand command, CardRuntime card)> commandQueue = new();
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
        System.Action<CardRuntime> onShowCard,
        System.Action onHideCard,
        System.Action onExecutionComplete)
    {
        commandQueue.Clear();

        foreach (var selectedCardRuntime in selectedCards)
        {
            // このカードがアタッチされている特定の武器を取得する
            WeaponRuntime weaponRuntime = selectedCardRuntime.weaponRuntime;
            PlayerRuntime player = weaponRuntime.ParentPlayer;

            if (player == null || weaponRuntime == null)
            {
                Debug.LogError($"カード {selectedCardRuntime.ID} ({selectedCardRuntime.InstanceID}) はどの武器にもアタッチされていません！");
                Debug.LogError($"武器 {weaponRuntime.ID} ({weaponRuntime.InstanceID}) はどのプレイヤーにも所持されていません！");
                continue;
            }

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
                HandleSupportAction(player, selectedCardRuntime, playerStatusUIControllers);
            }
        }

        // 順に実行
        while (commandQueue.Count > 0)
        {
            var item = commandQueue.Dequeue();
            ICommand currentCommand = item.command;
            CardRuntime currentCard = item.card;

            onShowCard?.Invoke(currentCard);
            yield return coroutineRunner.StartCoroutine(currentCommand.Do());
            bool isNextSameCard = false;
            if (commandQueue.Count > 0)
            {
                var nextItem = commandQueue.Peek();
                if (nextItem.card == currentCard)
                {
                    isNextSameCard = true;
                }
            }
            if (!isNextSameCard)
            {
                onHideCard?.Invoke();
            }
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
            case AttributeType.AttackBuff:
            case AttributeType.DefenseBuff:
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

        List<EnemyRuntime> actualTargets = new List<EnemyRuntime>();                            // 攻撃対象リスト
        CardModel cardModel = cardModelFactory.CreateFromID(selectedCardRuntime.ID);            // 攻撃範囲(TargetScope)を確認する

        if (cardModel.TargetScope == CardTargetScope.All)
        {
            // --- 全体攻撃 ---
            // 生存している敵全員をリストに入れる
            actualTargets = allEnemyRuntimes.Where(r => r.CurrentHP > 0).ToList();

            if (actualTargets.Count == 0)
            {
                Debug.LogWarning("全体攻撃ですが、生存している敵がいません。");
            }
        }
        else
        {
            // --- 単体攻撃 (従来のロジック) ---
            // 選択されたターゲットリストから、生存している最初の1体を探す
            EnemyRuntime targetRuntime = null;
            foreach (var potentialTargetModel in targets)
            {
                var runtime = allEnemyRuntimes.FirstOrDefault(r => r.EnemyModel == potentialTargetModel);
                if (runtime != null && runtime.CurrentHP > 0)
                {
                    targetRuntime = runtime;
                    break; // 1体見つかったら確定
                }
            }
            if (targetRuntime != null)
            {
                actualTargets.Add(targetRuntime);
            }
        }

        // 各ターゲットに対して攻撃コマンドを実行
        foreach (var targetRuntime in actualTargets)
        {
            EnemyStatusUIController targetEnemyUI = enemyStatusUIControllers.FirstOrDefault(ui => ui.GetEnemyModel() == targetRuntime.EnemyModel);

            if (targetEnemyUI != null && enemyControllers.TryGetValue(targetRuntime.EnemyModel, out EnemyController targetEnemyController))
            {
                Transform targetTransform = targetEnemyController.transform;
                var command = new AttackCommand(
                    attackPlayer,
                    weaponRuntime,
                    selectedCardRuntime,
                    targetEnemyUI,
                    attackerUI,
                    targetRuntime,
                    targetTransform,
                    damageCalculator,
                    cardModelFactory);
                commandQueue.Enqueue((command, selectedCardRuntime));
            }
            else
            {
                Debug.LogError($"Target UI or Controller not found for EnemyID: {targetRuntime.ID}");
            }
        }

        if (actualTargets.Count == 0)
        {
            Debug.LogWarning($"プレイヤー {attackPlayer.PlayerModel.PlayerName} の攻撃対象がいません。");
        }
    }

    /// <summary>
    /// 援属性カードの処理（コマンドをキューに追加）
    /// </summary>
    private void HandleSupportAction(PlayerRuntime player, CardRuntime selectedCardRuntime, List<PlayerStatusUIController> playerStatusUIControllers)
    {
        PlayerStatusUIController attackerUI = playerStatusUIControllers.FirstOrDefault(ui => ui.GetPlayerRuntime() == player);
        CardModel cardModel = cardModelFactory.CreateFromID(selectedCardRuntime.ID);
        ICommand command = null;
        switch (selectedCardRuntime.attribute)
        {
            case AttributeType.Heal:
                command = new HealCommand(player, selectedCardRuntime, attackerUI, cardModel);
                break;
            case AttributeType.AttackBuff:  // 攻撃バフ
            case AttributeType.DefenseBuff: // 防御バフ
                command = new BuffCommand(player, selectedCardRuntime, attackerUI, cardModel);
                break;
        }

        if (command != null)
        {
            // ★変更: コマンドとカードをセットでキューに入れる
            commandQueue.Enqueue((command, selectedCardRuntime));
        }
    }
}