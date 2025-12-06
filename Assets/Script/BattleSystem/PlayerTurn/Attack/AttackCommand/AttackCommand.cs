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
    private EnemyStatusUIController enemyStatusUIController;
    private CardModelFactory cardModelFactory;
    private Transform targetTransform;
    private DamageCalculator damageCalculator;

    public AttackCommand(PlayerRuntime player, WeaponRuntime weapon, CardRuntime card, EnemyStatusUIController enemyStatusUIController,
                            EnemyModel enemy, Transform targetTransform, DamageCalculator damageCalculator, CardModelFactory cardModelFactory)
    {
        this.targetEnemy = enemy;
        this.player = player;
        this.card = card;
        this.weapon = weapon;
        this.enemyStatusUIController = enemyStatusUIController;
        this.cardModelFactory = cardModelFactory;
        this.targetTransform = targetTransform;
        this.damageCalculator = damageCalculator;
    }

    public IEnumerator Do()
    {
        Debug.Log($"[AttackCommand] Do() 実行開始。 CardID: {card.ID}, TargetEnemy: {targetEnemy.EnemyID}");

        PlayerController controller = player.PlayerController;
        CardModel cardModel = cardModelFactory.CreateFromID(card.ID);

        yield return controller.AttackSequence(cardModel, weapon, targetTransform);

        float damage = damageCalculator.CalculateFinalDamage(player, weapon, card, targetEnemy);

        //Card属性ごとの効果音s
        attackedSoundEffect(card.attribute);
        // ターゲットのHPを減算
        targetEnemy.HPHandler.TakeDamage(damage);
        enemyStatusUIController.UpdateHP(targetEnemy.EnemyHP);
        // ターゲットに状態異常を付与
        ApplyStatusEffectToEnemy(cardModel, targetEnemy);
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
        if (attribute == AttributeType.Slash) SoundManager.Instance.PlaySE(SEType.SlashAttack);
        else if (attribute == AttributeType.Blunt) SoundManager.Instance.PlaySE(SEType.BluntAttack);
        else if (attribute == AttributeType.Bullet) SoundManager.Instance.PlaySE(SEType.BulletAttack);
        else if (attribute == AttributeType.Pierce) SoundManager.Instance.PlaySE(SEType.PierceAttack);
    }

    /// <summary>
    /// カードに設定されたステータス効果を敵に適用する
    /// </summary>
    private void ApplyStatusEffectToEnemy(CardModel cardModel, EnemyModel enemy)
    {
        if (cardModel == null || cardModel.StatusEffect.Type == StatusEffectType.None)
        {
            Debug.Log("[AttackCommand]カードに状態異常効果が設定されていないため、適用をスキップ。");
            return;
        }

        StatusEffect newEffect = new StatusEffect(
            cardModel.StatusEffect.Type,            // 破砕、熔鉄など
            cardModel.StatusEffect.Value,           // 効果値
            cardModel.StatusEffect.Duration,        // 持続ターン
            cardModel.StatusEffect.InflictStacks    // 付与するスタック数
        );

        // 敵のStatusHandlerに適用（ここでスタックが加算される）
        if (enemy.StatusHandler != null)
        {
            enemy.StatusHandler.ApplyStatus(newEffect);

            // UI更新が必要な場合はここで呼ぶ
            // enemyStatusUIController.UpdateStatusIcons(enemy.StatusHandler); 

            Debug.Log($"敵にデバフ付与: {newEffect.Type} を {newEffect.StackCount} スタック");
        }
    }
}
