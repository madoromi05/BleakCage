using UnityEngine;
using System.Collections;

/// <summary>
/// 選択したカードが敵に攻撃するコマンド
///</summary>
public class AttackCommand : ICommand
{
    private PlayerRuntime player;
    private EnemyModel targetEnemy;
    private CardRuntime card;
    private WeaponRuntime weapon;
    private IAttackStrategy damageStrategy;
    private EnemyStatusUIController enemyStatusUIController;
    private CardModelFactory cardModelFactory;

    public AttackCommand(PlayerRuntime player, WeaponRuntime weapon, CardRuntime card,EnemyStatusUIController enemyStatusUIController,
                            EnemyModel enemy, IAttackStrategy strategy, CardModelFactory cardModelFactory)
    {
        this.damageStrategy = strategy;
        this.targetEnemy = enemy;
        this.player = player;
        this.card = card;
        this.weapon = weapon;
        this.enemyStatusUIController = enemyStatusUIController;
        this.cardModelFactory = cardModelFactory;
    }

    public IEnumerator Do()
    {
        if (targetEnemy.EnemyHP <= 0)
        {
            Debug.Log($" EnemyID： {targetEnemy.EnemyID} は既に倒されているため、攻撃をスキップしました。");
            yield break;
        }

        // 1. PlayerRuntime から PlayerController の実体を取得
        PlayerController controller = player.PlayerController;
        if (controller == null)
        {
            Debug.LogError($"Player (ID: {player.ID}) の PlayerController が null です！");
            yield break;
        }
        // 2. CardRuntime の ID を使って、CardModel (マスターデータ) を取得
        CardModel cardModel = cardModelFactory.CreateFromID(card.ID);

        // 4. CardModel からアニメーションクリップを取得し、再生
        controller.PlayAttackAnimation(cardModel.AttackAnimation);

        float waitTime = 0.5f; // デフォルトの待機時間 (クリップが null だった場合)
        if (cardModel.AttackAnimation != null)
        {
            waitTime = cardModel.AttackAnimation.length;
        }

        yield return new WaitForSeconds(waitTime);

        float damage = damageStrategy.CalculateFinalDamage(player, weapon, card , targetEnemy);

        // ターゲットのHPを減算
        targetEnemy.EnemyHP -= damage;
        enemyStatusUIController.UpdateHP(targetEnemy.EnemyHP);

        // 結果をログに出力
        Debug.Log($" EnemyID： {targetEnemy.EnemyID} に player;{player.ID}がweapon:{weapon.ID}とcard:{card.ID}で{damage:F2} ダメージを与えた。残りHP: {targetEnemy.EnemyHP:F2}");

        yield return new WaitForSeconds(waitTime);
    }

    public bool Undo()
    {
        Debug.Log("[AttackCardCommand] Undo not implemented.");
        return false;
    }
}
