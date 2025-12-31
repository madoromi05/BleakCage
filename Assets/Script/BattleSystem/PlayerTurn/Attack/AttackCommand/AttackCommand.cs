using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// 選択したカードが敵に攻撃するコマンド
///</summary>
public class AttackCommand : ICommand
{
    private PlayerRuntime player;
    private EnemyRuntime targetEnemy;
    private CardRuntime card;
    private WeaponRuntime weapon;
    private EnemyStatusUIController enemyStatusUIController;
    private PlayerStatusUIController playerStatusUIController;
    private CardModelFactory cardModelFactory;
    private Transform targetTransform;
    private DamageCalculator damageCalculator;

    public AttackCommand(PlayerRuntime player, WeaponRuntime weapon, CardRuntime card,
                            EnemyStatusUIController enemyStatusUIController, PlayerStatusUIController playerStatusUIController,
                            EnemyRuntime enemy, Transform targetTransform, DamageCalculator damageCalculator, CardModelFactory cardModelFactory)
    {
        this.targetEnemy = enemy;
        this.playerStatusUIController = playerStatusUIController;
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
        DebugCostom.Log($"[AttackCommand] 実行開始。 CardID: {card.ID}, Attribute: {card.attribute} (これがBuffなら設定ミスです)");
        if (weapon == null)
        {
            DebugCostom.LogWarning("[AttackCommand] WeaponRuntime is null. Using default or aborting.");
        }
        PlayerController controller = player.PlayerController;
        CardModel cardModel = cardModelFactory.CreateFromID(card.ID);

        // 攻撃ヒット時の処理を定義（ローカル関数）
        int hitCount = 0;
        Action onHitAction = () =>
        {
            // 敵が生きていればダメージ処理
            if (targetEnemy.CurrentHP > 0)
            {
                hitCount++;

                // ダメージ計算
                float damage = damageCalculator.CalculateFinalDamage(player, weapon, card, targetEnemy);

                // 効果音
                attackedSoundEffect(card.attribute);        // 味方の攻撃音
                PlayEnemyDamageSound(card.attribute);       // 敵の被弾音
                // ターゲットのHPを減算
                targetEnemy.HpHandler.TakeDamage(damage);
                enemyStatusUIController.UpdateHP(targetEnemy.CurrentHP);

                // 状態異常の付与
                ApplyStatusEffectToEnemy(cardModel, targetEnemy);

                DebugCostom.Log($"[{hitCount}ヒット目] EnemyID：{targetEnemy.ID} に {damage:F2} ダメージ");
            }
        };

        // イベントを購読 (Subscribe)
        controller.OnAttackHitTriggered += onHitAction;

        // アニメーションシーケンスの実行
        yield return controller.AttackSequence(cardModel, weapon, targetTransform);

        controller.OnAttackHitTriggered -= onHitAction;
        yield return new WaitForSeconds(0.1f);
    }

    public bool Undo()
    {
        DebugCostom.LogError("[AttackCardCommand] Undo not implemented.");
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
    private void ApplyStatusEffectToEnemy(CardModel cardModel, EnemyRuntime enemy)
    {
        StatusEffect newEffect = new StatusEffect(
            cardModel.StatusEffect.Type,            // 破砕、熔鉄など
            cardModel.StatusEffect.Value,           // 効果値
            cardModel.StatusEffect.Duration,        // 持続ターン
            cardModel.StatusEffect.InflictStacks    // 付与するスタック数
        );

        switch (newEffect.Type)
        {
            case StatusEffectType.None:
                return;

            // --- 敵 (EnemyRuntime) に付与する異常状態 ---
            case StatusEffectType.Fracture:     // 【破砕】: 防御貫通UP
            case StatusEffectType.Laceration:   // 【損傷】: ターン終了時ダメージ
            case StatusEffectType.Meltdown:     // 【熔鉄】: 攻防ダウン
                if (targetEnemy.StatusHandler != null)
                {
                    targetEnemy.StatusHandler.ApplyStatus(newEffect);
                    enemyStatusUIController.UpdateStatusIcons(targetEnemy.StatusHandler);
                    DebugCostom.Log($"[Debuff] 敵(ID:{targetEnemy.ID})に {newEffect.Type} を付与");
                }
                break;

            // --- 自分 (PlayerRuntime) に付与するバフ/状態 ---
            case StatusEffectType.Cover:        // 【援護】: ダメージ無効化
            case StatusEffectType.Target:       // 【目標】: 命中率UP
            case StatusEffectType.DefenceUp:    // 【防御力UP】
            case StatusEffectType.AttackUp:     // 【攻撃力UP】       
                if (player.StatusHandler != null)
                {
                    player.StatusHandler.ApplyStatus(newEffect);
                    playerStatusUIController.UpdateStatusIcons(player.StatusHandler);
                    DebugCostom.Log($"[Buff] プレイヤー(ID:{player.ID})に {newEffect.Type} を付与");
                }
                break;

            default:
                DebugCostom.LogWarning($"[AttackCommand] 未対応のステータスタイプです: {newEffect.Type}");
                break;
        }
    }

    private void PlayEnemyDamageSound(AttributeType attribute)
    {
        switch (attribute)
        {
            case AttributeType.Bullet:
                SoundManager.Instance.PlaySE(SEType.damagedBulletEnemy);
                break;
            case AttributeType.Pierce:
                SoundManager.Instance.PlaySE(SEType.damagedPierceEnemy);
                break;
            case AttributeType.Blunt:
                SoundManager.Instance.PlaySE(SEType.damagedBluntEnemy);
                break;
            case AttributeType.Slash:
                SoundManager.Instance.PlaySE(SEType.damagedSlashEnemy);
                break;
            default:
                // デフォルト
                SoundManager.Instance.PlaySE(SEType.damagedBluntEnemy);
                break;
        }
    }
}
