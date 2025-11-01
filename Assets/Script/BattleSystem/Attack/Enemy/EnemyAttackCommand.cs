using System.Collections;
using UnityEngine;

public class EnemyAttackCommand : ICommand
{
    private PlayerModel player;
    private EnemyModel enemy;
    private IEnemyAttackStrategy damageStrategy;
    private PlayerStatusUIController playerStatusUIController;

    private DefenseResult defenseResult = DefenseResult.None;
    public PlayerModel PlayerTarget => player;

    public EnemyAttackCommand(PlayerModel player, EnemyModel enemy, IEnemyAttackStrategy attackStrategy,
                                PlayerStatusUIController playerStatusUIController)
    {
        this.player = player;
        this.enemy = enemy;
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
        Debug.Log($"攻撃実行: Enemy='{enemy.EnemyID}' が " +
                  $"Player='{player.PlayerID}' に攻撃！");

        float baseDamage = damageStrategy.CalculateFinalDamage(enemy, player);
        float finalDamage = 0f;

        // 防御結果に応じて最終ダメージを決定 (ゲージ処理は削除)
        switch (defenseResult)
        {
            case DefenseResult.Counter:
                finalDamage = 0f;
                Debug.Log("カウンター成功！ ダメージ 0！");
                break;

            case DefenseResult.Guard:
                finalDamage = baseDamage * 0.5f;
                Debug.Log("ガード成功！ ダメージ軽減！");
                break;

            default: // DefenseResult.None
                finalDamage = baseDamage;
                Debug.Log("被弾！");
                break;
        }

        // ターゲットのHPを減算
        player.PlayerHP -= finalDamage;
        playerStatusUIController.UpdateHP(player.PlayerHP);

        Debug.Log($"[EnemyAttackCardCommand] {player.PlayerName} に {finalDamage:F2} ダメージを与えた。残りHP: {player.PlayerHP:F2}");

        yield break;
    }

    public bool Undo()
    {
        Debug.Log("[EnemyAttackCardCommand] Undo not implemented.");
        return false;
    }
}