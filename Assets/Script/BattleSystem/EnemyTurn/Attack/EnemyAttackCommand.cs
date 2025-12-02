using System.Collections;
using UnityEngine;

public class EnemyAttackCommand : ICommand
{
    public PlayerRuntime PlayerTarget { get; }
    public EnemyModel Attacker { get; }
    private EnemyAttackDamage calculator = new EnemyAttackDamage();
    private EnemyController _enemyController;
    private PlayerStatusUIController _playerStatusUIController;

    public EnemyAttackCommand(PlayerRuntime player, EnemyModel enemy,
                              EnemyController enemyController,
                              PlayerController playerController,
                              PlayerStatusUIController playerStatusUIController)
    {
        this.PlayerTarget = player;
        this.Attacker = enemy;
        _enemyController = enemyController;
        _playerStatusUIController = playerStatusUIController;
    }

    /// <summary>
    /// 敵の攻撃アニメーションを再生し、終わるまで待機する
    /// (ダメージ処理はここでは行わない)
    /// </summary>
    public IEnumerator Do()
    {
        Debug.Log($"攻撃実行: Enemy='{Attacker.EnemyID}' が Player='{PlayerTarget.PlayerModel.PlayerID}' に攻撃開始！");

        // 1. 敵の攻撃アニメーションを再生し、その長さを取得する
        // (このアニメーションの途中で 'OnAttackHitMoment' イベントが発火する)
        float attackAnimTime = 0.5f;
        if (_enemyController != null)
        {
            attackAnimTime = _enemyController.PlayRandomAttackAnimation();
        }

        // 2. アニメーションが終了するまで待機する
        yield return new WaitForSeconds(attackAnimTime);

        // 3. アニメーション再生後 (ダメージ処理は EnemyTurn が行う)
        Debug.Log($"攻撃アニメ終了: Enemy='{Attacker.EnemyID}'");
    }

    /// <summary>
    ///  EnemyTurn から呼び出される実際のダメージ処理
    /// </summary>
    public void ApplyDamageAfterJudgement()
    {
        // ダメージを計算する
        float damage = calculator.CalculateFinalDamage(
            Attacker,
            PlayerTarget.PlayerModel
        );

        // 2. HPを減算する
        SoundManager.Instance.PlaySE(SEType.damagedPlayer);
        PlayerTarget.HPHandler.TakeDamage(damage);
        _playerStatusUIController.UpdateHP(PlayerTarget.CurrentHP);

        Debug.Log($"[EnemyAttackCardCommand] {PlayerTarget.PlayerModel.PlayerName} に {damage:F2} ダメージを与えた。残りHP: {PlayerTarget.CurrentHP:F2}");
    }

    public bool Undo()
    {
        Debug.Log("[EnemyAttackCardCommand] Undo not implemented.");
        return false;
    }
}