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
        var damageSystemGo = new GameObject("TempDamageCalc");
        var damageSystem = damageSystemGo.AddComponent<AttributeWeakness>();

        damageSystem.weakAgainstAttribute = card.CardAttribute;                                     // カードの属性
        damageSystem.attackerPower = player.PlayerAttackPower;                                      // プレイヤーの攻撃力
        damageSystem.weaponPower = weapon.WeaponAttackPower;                                        // 武器の攻撃力
        damageSystem.peakyCoefficient = weapon.PeakyCoefficient;                                    // ピーキー係数
        damageSystem.defenderPower = enemy.EnemyDefensePower;                                       // 敵の防御力
        damageSystem.weakAgainstAttribute = card.CardAttribute;                                     // カードの属性
        damageSystem.weakAgainstCharacterType = (DefensAttributeType)enemy.EnemyDefensAttribute;    // 敵の防御属性

        Debug.Log($"[AttackCardCommand] " +
          $"プレイヤー: {player.PlayerName}, " +
          $"プレイヤー攻撃力: {damageSystem.attackerPower}, " +
          $"武器攻撃力: {damageSystem.weaponPower}, " +
          $"ピーキー係数: {damageSystem.peakyCoefficient}, " +
          $"敵の防御力: {damageSystem.defenderPower}, " +
          $"カード属性: {damageSystem.weakAgainstAttribute}, " +
          $"敵の防御属性: {damageSystem.weakAgainstCharacterType}");

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
