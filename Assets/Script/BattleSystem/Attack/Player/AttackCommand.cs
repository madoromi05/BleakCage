using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
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

    public AttackCommand(PlayerRuntime player, WeaponRuntime weapon, CardRuntime card, EnemyModel enemy, IAttackStrategy attackStrategy)
    {
        this.player = player;
        this.targetEnemy = enemy;
        this.card = card;
        this.weapon = weapon;
        this.damageStrategy = attackStrategy;
    }

    public bool Do()
    {
        float damage = damageStrategy.CalculateFinalDamage(player, weapon, card , targetEnemy);

        // ターゲットのHPを減算
        targetEnemy.EnemyHP -= damage;

        // 結果をログに出力
        Debug.Log($"[AttackCardCommand] {targetEnemy.EnemyID} に player;{player.ID}がweapon:{weapon.ID}とcard:{card.ID}で{damage:F2} ダメージを与えた。残りHP: {targetEnemy.EnemyHP:F2}");

        return true;
    }

    public bool Undo()
    {
        Debug.Log("[AttackCardCommand] Undo not implemented.");
        return false;
    }
}
