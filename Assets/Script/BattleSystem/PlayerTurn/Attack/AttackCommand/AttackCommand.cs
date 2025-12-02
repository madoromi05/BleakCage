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
    private Transform targetTransform;

    public AttackCommand(PlayerRuntime player, WeaponRuntime weapon, CardRuntime card,EnemyStatusUIController enemyStatusUIController,
                            EnemyModel enemy, Transform targetTransform, IAttackStrategy strategy, CardModelFactory cardModelFactory)
    {
        this.damageStrategy = strategy;
        this.targetEnemy = enemy;
        this.player = player;
        this.card = card;
        this.weapon = weapon;
        this.enemyStatusUIController = enemyStatusUIController;
        this.cardModelFactory = cardModelFactory;
        this.targetTransform = targetTransform;
    }

    public IEnumerator Do()
    {
        Debug.Log($"[AttackCommand] Do() 実行開始。 CardID: {card.ID}, TargetEnemy: {targetEnemy.EnemyID}");

        PlayerController controller = player.PlayerController;
        CardModel cardModel = cardModelFactory.CreateFromID(card.ID);

        yield return controller.AttackSequence(cardModel, weapon, targetTransform);

        float damage = damageStrategy.CalculateFinalDamage(player, weapon, card, targetEnemy);

        //Card属性ごとの効果音s
        attackedSoundEffect(card.attribute);
        // ターゲットのHPを減算
        targetEnemy.HPHandler.TakeDamage(damage);
        enemyStatusUIController.UpdateHP(targetEnemy.EnemyHP);

        Debug.Log($" EnemyID： {targetEnemy.EnemyID} に player;{player.ID}がweapon:{weapon.ID}とcard:{card.ID}で{damage:F2} ダメージを与えた。残りHP: {targetEnemy.EnemyHP:F2}");

        // アニメーション後の硬直時間
        yield return new WaitForSeconds(0.1f);
    }

    public bool Undo()
    {
        Debug.Log("[AttackCardCommand] Undo not implemented.");
        return false;
    }
    private void attackedSoundEffect(AttributeType attribute)
    {
        if(attribute == AttributeType.Slash)  SoundManager.Instance.PlaySE(SEType.SlashAttack);
        else if(attribute == AttributeType.Blunt)  SoundManager.Instance.PlaySE(SEType.BluntAttack);
        else if(attribute == AttributeType.Bullet)  SoundManager.Instance.PlaySE(SEType.BulletAttack);
        else if(attribute == AttributeType.Pierce)  SoundManager.Instance.PlaySE(SEType.PierceAttack);
    }
}
