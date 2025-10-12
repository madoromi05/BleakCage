using UnityEngine;

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

    public AttackCommand(PlayerRuntime player, WeaponRuntime weapon, CardRuntime card,EnemyStatusUIController enemyStatusUIController,
                            EnemyModel enemy, IAttackStrategy strategy)
    {
        this.damageStrategy = strategy;
        this.targetEnemy = enemy;
        this.player = player;
        this.card = card;
        this.weapon = weapon;
        this.enemyStatusUIController = enemyStatusUIController;
    }

    public bool Do()
    {
        float damage = damageStrategy.CalculateFinalDamage(player, weapon, card , targetEnemy);

        // ターゲットのHPを減算
        targetEnemy.EnemyHP -= damage;
        enemyStatusUIController.UpdateHP(targetEnemy.EnemyHP);

        // 結果をログに出力
        Debug.Log($" EnemyID： {targetEnemy.EnemyID} に player;{player.ID}がweapon:{weapon.ID}とcard:{card.ID}で{damage:F2} ダメージを与えた。残りHP: {targetEnemy.EnemyHP:F2}");

        return true;
    }

    public bool Undo()
    {
        Debug.Log("[AttackCardCommand] Undo not implemented.");
        return false;
    }
}
