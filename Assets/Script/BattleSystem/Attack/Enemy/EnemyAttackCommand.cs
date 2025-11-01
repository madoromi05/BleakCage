using System.Collections;
using UnityEngine;

public class EnemyAttackCommand : ICommand
{
    public PlayerModel PlayerTarget { get; }
    public EnemyModel Attacker { get; }
    public IEnemyAttackStrategy DamageStrategy { get; }
    private EnemyController enemyController;
    private PlayerStatusUIController playerStatusUIController;

    private DefenseResult defenseResult = DefenseResult.None;

    public EnemyAttackCommand(PlayerModel player, EnemyModel enemy,
                              EnemyController enemyController,
                              PlayerController playerController,
                              IEnemyAttackStrategy attackStrategy,
                              PlayerStatusUIController playerStatusUIController)
    {
        this.PlayerTarget = player; // 外部(EnemyTurn)から参照できるように
        this.Attacker = enemy;     // 外部(EnemyTurn)から参照できるように
        this.enemyController = enemyController;
        this.DamageStrategy = attackStrategy; // 外部(EnemyTurn)から参照できるように
        this.playerStatusUIController = playerStatusUIController;
    }

    /// <summary>
    /// 敵の攻撃アニメーションを再生し、終わるまで待機する
    /// (ダメージ処理はここでは行わない)
    /// </summary>
    public IEnumerator Do()
    {
        Debug.Log($"攻撃実行: Enemy='{Attacker.EnemyID}' が Player='{PlayerTarget.PlayerID}' に攻撃開始！");

        // 1. 敵の攻撃アニメーションを再生し、その長さを取得する
        // (このアニメーションの途中で 'OnAttackHitMoment' イベントが発火する)
        float attackAnimTime = 0.5f; // デフォルト
        if (enemyController != null)
        {
            attackAnimTime = enemyController.PlayRandomAttackAnimation();
        }

        // 2. アニメーションが終了するまで待機する
        yield return new WaitForSeconds(attackAnimTime);

        // 3. アニメーション再生後 (ダメージ処理は EnemyTurn が行う)
        Debug.Log($"攻撃アニメ終了: Enemy='{Attacker.EnemyID}'");
    }

    /// <summary>
    /// ★ EnemyTurn から呼び出される実際のダメージ処理
    /// </summary>
    public void ApplyDamageAfterJudgement()
    {
        // アニメーションイベントによる判定後、EnemyTurn がこの関数を呼び出す

        // 1. ダメージを計算する (ガード/カウンターは EnemyTurn が判定済み)
        float baseDamage = DamageStrategy.CalculateFinalDamage(Attacker, PlayerTarget);

        // 2. HPを減算する
        PlayerTarget.PlayerHP -= baseDamage;
        playerStatusUIController.UpdateHP(PlayerTarget.PlayerHP);

        Debug.Log($"[EnemyAttackCardCommand] {PlayerTarget.PlayerName} に {baseDamage:F2} ダメージを与えた。残りHP: {PlayerTarget.PlayerHP:F2}");
    }

    public bool Undo()
    {
        Debug.Log("[EnemyAttackCardCommand] Undo not implemented.");
        return false;
    }
}