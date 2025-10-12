using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 選択したカードがプレイヤーに攻撃するコマンド
///</summary>
public class EnemyAttackCommand : ICommand
{
    private PlayerModel player;
    private EnemyModel enemy;
    private IEnemyAttackStrategy damageStrategy;
    private PlayerStatusUIController playerStatusUIController;

    public EnemyAttackCommand(PlayerModel player, EnemyModel enemy, IEnemyAttackStrategy attackStrategy, 
                                 PlayerStatusUIController playerStatusUIController)
    {
        this.player = player;
        this.enemy = enemy;
        this.damageStrategy = attackStrategy;
        this.playerStatusUIController = playerStatusUIController;
    }

    public bool Do()
    {
        Debug.Log($"攻撃実行: Enemy='{enemy.EnemyID}' が " +
                 $"Player='{player.PlayerID}' に攻撃！");
        float damage = damageStrategy.CalculateFinalDamage(enemy, player);

        // ターゲットのHPを減算
        player.PlayerHP -= damage;
        playerStatusUIController.UpdateHP(player.PlayerHP);

        // 結果をログに出力
        Debug.Log($"[EnemyAttackCardCommand] {player.PlayerName} に {damage:F2} ダメージを与えた。残りHP: {player.PlayerHP:F2}");

        return true;
    }

    public bool Undo()
    {
        Debug.Log("[EnemyAttackCardCommand] Undo not implemented.");
        return false;
    }
}
