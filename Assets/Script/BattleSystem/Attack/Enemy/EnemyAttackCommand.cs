using System.Collections;
using UnityEngine;

public class EnemyAttackCommand : ICommand
{
    private PlayerModel player;
    private EnemyModel enemy;
    private EnemyController enemyController;
    private PlayerController playerController;
    private IEnemyAttackStrategy damageStrategy;
    private PlayerStatusUIController playerStatusUIController;

    private DefenseResult defenseResult = DefenseResult.None;
    public PlayerModel PlayerTarget => player;

    public EnemyAttackCommand(PlayerModel player, EnemyModel enemy,
                              EnemyController enemyController,
                              PlayerController playerController,
                              IEnemyAttackStrategy attackStrategy,
                              PlayerStatusUIController playerStatusUIController)
    {
        this.player = player;
        this.enemy = enemy;
        this.enemyController = enemyController;
        this.playerController = playerController;
        this.damageStrategy = attackStrategy;
        this.playerStatusUIController = playerStatusUIController;
    }

    /// <summary>
    /// EnemyTurnから防御結果を設定するメソッド
    /// </summary>
    public void SetDefenseResult(DefenseResult result)
    {
        this.defenseResult = result;
    }

    public IEnumerator Do()
    {
        Debug.Log($"攻撃実行: Enemy='{enemy.EnemyID}' が Player='{player.PlayerID}' に攻撃！");

        // 1. ダメージを先に計算する
        float baseDamage = damageStrategy.CalculateFinalDamage(enemy, player);
        float finalDamage = 0f;

        switch (defenseResult) // HP計算
        {
            case DefenseResult.Counter:
                finalDamage = 0f;
                break;
            case DefenseResult.Guard:
                finalDamage = baseDamage * 0.5f;
                break;
            default:
                finalDamage = baseDamage;
                break;
        }

        // 2. アニメーションを再生する
        float attackAnimTime = 0.5f; // デフォルト
        float playerAnimTime = 0.0f; // デフォルト

        // 2a. 敵の攻撃アニメ
        if (enemyController != null)
        {
            attackAnimTime = enemyController.PlayRandomAttackAnimation();
        }
        // --- [修正ここまで] ---

        // 2b. プレイヤーの防御/被弾アニメ
        switch (defenseResult)
        {
            case DefenseResult.Counter:
                Debug.Log("カウンター成功！");
                if (playerController != null)
                {
                    playerController.PlayGuardAnimation();
                    playerAnimTime = playerController.GetGuardAnimationLength();
                }
                break;

            case DefenseResult.Guard:
                Debug.Log("ガード成功！");
                if (playerController != null)
                {
                    playerController.PlayGuardAnimation();
                    playerAnimTime = playerController.GetGuardAnimationLength();
                }
                break;

            default:
                Debug.Log("被弾！ (アニメーションなし)");
                playerAnimTime = 0.0f; // 被弾アニメがないため待機時間 0
                break;
        }

        // 3. 2つのアニメーションのうち
        float waitTime = Mathf.Max(attackAnimTime, playerAnimTime);
        yield return new WaitForSeconds(waitTime);

        // 4. アニメーション再生後、HPを減算する
        player.PlayerHP -= finalDamage;
        playerStatusUIController.UpdateHP(player.PlayerHP);

        Debug.Log($"[EnemyAttackCardCommand] {player.PlayerName} に {finalDamage:F2} ダメージを与えた。残りHP: {player.PlayerHP:F2}");
        yield return new WaitForSeconds(0.1f);
    }

    public bool Undo()
    {
        Debug.Log("[EnemyAttackCardCommand] Undo not implemented.");
        return false;
    }
}