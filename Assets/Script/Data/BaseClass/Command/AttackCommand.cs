using UnityEngine;

public class AttackCardCommand : ICardCommand
{
    private PlayerModel player;
    private EnemyModel enemy;
    private CardModel card;
    private WeaponModel weapon;

    public AttackCardCommand(PlayerModel player, EnemyModel enemy, CardModel card, WeaponModel weapon)
    {
        this.player = player;
        this.enemy = enemy;
        this.card = card;
        this.weapon = weapon;
    }

    public bool Do()
    {
        if (player == null)
        {
            Debug.LogError("[AttackCardCommand] player is null");
            return false;
        }
        if (enemy == null)
        {
            Debug.LogError("[AttackCardCommand] enemy is null");
            return false;
        }
        if (card == null)
        {
            Debug.LogError("[AttackCardCommand] card is null");
            return false;
        }
        if (weapon == null)
        {
            Debug.LogError("[AttackCardCommand] weapon is null");
            return false;
        }

        if (enemy.EnemyName == null)
        {
            Debug.LogError("[AttackCardCommand] enemy.EnemyName is null");
            return false;
        }

        var damageSystemGo = new GameObject("TempDamageCalc");
        if (damageSystemGo == null)
        {
            Debug.LogError("[AttackCardCommand] failed to create TempDamageCalc GameObject");
            return false;
        }
        var damageSystem = damageSystemGo.AddComponent<AttributeWeakness>();
        if (damageSystem == null)
        {
            Debug.LogError("[AttackCardCommand] failed to add AttributeWeakness component");
            return false;
        }

        damageSystem.attackerPower = player.PlayerAttackPower;
        damageSystem.weaponPower = weapon.WeaponAttackPower;
        damageSystem.peakyCoefficient = weapon.PeakyCoefficient;
        damageSystem.defenderPower = enemy.EnemyDefensePower;
        damageSystem.weakAgainstAttribute = card.CardAttribute;
        damageSystem.weakAgainstCharacterType = (DefensAttributeType)enemy.EnemyAttribute;

        damageSystem.CalculateDamage();

        float damage = damageSystem.finalDamage;

        enemy.EnemyHP -= damage;
        Debug.Log($"[AttackCardCommand] {enemy.EnemyName} に {damage} ダメージ。残HP: {enemy.EnemyHP}");

        GameObject.Destroy(damageSystemGo);
        return true;
    }

    public bool Undo()
    {
        Debug.Log("[AttackCardCommand] Undo not implemented.");
        return false;
    }
}
