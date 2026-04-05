using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// 選択したカードが敵に攻撃するコマンド
///</summary>
public class AttackCommand : ICommand
{
    private PlayerRuntime _player;
    private EnemyRuntime _targetEnemy;
    private CardRuntime _card;
    private WeaponRuntime _weapon;
    private EnemyStatusUIController _enemyStatusUIController;
    private PlayerStatusUIController _playerStatusUIController;
    private CardModelFactory _cardModelFactory;
    private Transform _targetTransform;
    private DamageCalculator _damageCalculator;

    public AttackCommand(PlayerRuntime player, WeaponRuntime weapon, CardRuntime card,
                            EnemyStatusUIController enemyStatusUIController, PlayerStatusUIController playerStatusUIController,
                            EnemyRuntime enemy, Transform targetTransform, DamageCalculator damageCalculator, CardModelFactory cardModelFactory)
    {
        this._targetEnemy = enemy;
        this._playerStatusUIController = playerStatusUIController;
        this._player = player;
        this._card = card;
        this._weapon = weapon;
        this._enemyStatusUIController = enemyStatusUIController;
        this._cardModelFactory = cardModelFactory;
        this._targetTransform = targetTransform;
        this._damageCalculator = damageCalculator;
    }

    public IEnumerator Do()
    {
        DebugCostom.Log($"[AttackCommand] 実行開始。 CardID: {_card.ID}, Attribute: {_card.attribute} (これがBuffなら設定ミスです)");
        if (_weapon == null)
        {
            DebugCostom.LogWarning("[AttackCommand] WeaponRuntime is null. Using default or aborting.");
        }
        PlayerController controller = _player.PlayerController;
        CardModel cardModel = _cardModelFactory.CreateFromID(_card.ID);

        // 攻撃ヒット時の処理を定義（ローカル関数）
        int hitCount = 0;
        Action onHitAction = () =>
        {
            // 敵が生きていればダメージ処理
            if (_targetEnemy.CurrentHP > 0)
            {
                hitCount++;

                // ダメージ計算
                float damage = _damageCalculator.CalculateFinalDamage(_player, _weapon, _card, _targetEnemy);

                // 効果音
                AttackedSoundEffect(_card.attribute);        // 味方の攻撃音
                PlayEnemyDamageSound(_card.attribute);       // 敵の被弾音
                // ターゲットのHPを減算
                _targetEnemy.HpHandler.TakeDamage(damage);
                _enemyStatusUIController.UpdateHP(_targetEnemy.CurrentHP);

                // 状態異常の付与
                ApplyStatusEffectToEnemy(cardModel, _targetEnemy);

                DebugCostom.Log($"[{hitCount}ヒット目] EnemyID：{_targetEnemy.ID} に {damage:F2} ダメージ");
            }
        };

        // イベントを購読 (Subscribe)
        controller.OnAttackHitTriggered += onHitAction;

        // アニメーションシーケンスの実行
        yield return controller.AttackSequence(cardModel, _weapon, _targetTransform);

        controller.OnAttackHitTriggered -= onHitAction;
        yield return new WaitForSeconds(0.1f);
    }

    public bool Undo()
    {
        DebugCostom.LogError("[AttackCardCommand] Undo not implemented.");
        return false;
    }
    private void AttackedSoundEffect(AttributeType attribute)
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
                if (_targetEnemy.StatusHandler != null)
                {
                    _targetEnemy.StatusHandler.ApplyStatus(newEffect);
                    _enemyStatusUIController.UpdateStatusIcons(_targetEnemy.StatusHandler);
                    DebugCostom.Log($"[Debuff] 敵(ID:{_targetEnemy.ID})に {newEffect.Type} を付与");
                }
                break;

            // --- 自分 (PlayerRuntime) に付与するバフ/状態 ---
            case StatusEffectType.Cover:        // 【援護】: ダメージ無効化
            case StatusEffectType.Target:       // 【目標】: 命中率UP
            case StatusEffectType.DefenceUp:    // 【防御力UP】
            case StatusEffectType.AttackUp:     // 【攻撃力UP】       
                if (_player.StatusHandler != null)
                {
                    _player.StatusHandler.ApplyStatus(newEffect);
                    _enemyStatusUIController.UpdateStatusIcons(_targetEnemy.StatusHandler);
                    DebugCostom.Log($"[Buff] プレイヤー(ID:{_player.ID})に {newEffect.Type} を付与");
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
