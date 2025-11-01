using System.Collections;
using UnityEngine;

public class EnemyAttackCommand : ICommand
{
    private PlayerModel player;
    private EnemyModel enemy;
    private EnemyController enemyController;
    private PlayerController playerController; // 参照は残しますが、アニメーション再生には使いません
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
        // ★ 変更: プレイヤーアニメーションは常になし (0.0f)
        float playerAnimTime = 0.0f;

        // 2a. 敵の攻撃アニメ
        if (enemyController != null)
        {
            attackAnimTime = enemyController.PlayRandomAttackAnimation();
        }

        // 2b. プレイヤーの防御/被弾アニメ (★ すべてアニメーションなしに変更)
        switch (defenseResult)
        {
            case DefenseResult.Counter:
                Debug.Log("カウンター成功！ (エクストラターンへ)");
                playerAnimTime = 0.0f; // アニメーションなし
                break;

            case DefenseResult.Guard:
                Debug.Log("ガード成功！ (アニメーションなし)");
                playerAnimTime = 0.0f; // アニメーションなし
                break;

            default:
                Debug.Log("被弾！ (アニメーションなし)");
                playerAnimTime = 0.0f; // アニメーションなし
                break;
        }

        // 3. 2つのアニメーションのうち、長い方（＝実質、敵のアニメ時間）待機する
        // ★ 変更: playerAnimTime は 0.0f なので、実質 attackAnimTime だけ待つ
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