using UnityEngine;
using System.Collections;

/// <summary>
/// プレイヤーに防御バフを付与するコマンド
/// </summary>
public class DefenceCommand : ICommand
{
    private readonly PlayerRuntime player;
    private readonly CardRuntime card;

    public DefenceCommand(PlayerRuntime player, CardRuntime card)
    {
        this.player = player;
        this.card = card;
    }

    public IEnumerator Do()
    {
        PlayerModel playerModel = player.PlayerModel;

        // カードから防御値を取得
        // (例: OutputModifier が 0.2 なら 20% ダメージカット)
        float defenseValue = card.GetOutput();

        //「1ターン」持続（次のPlayerTurnの開始時まで）
        int durationTurns = 1;

        // 1. 新しいステータス効果を作成
        var defenceBuff = new StatusEffect(
            StatusEffectType.DefenceUp,
            defenseValue,
            durationTurns
        );

        // 2. プレイヤーのバフリストに追加
        player.BuffHandler.ApplyBuff(defenceBuff);

        yield break;
    }

    public bool Undo()
    {
        Debug.Log("[DefenceCommand] Undo not implemented.");
        return false;
    }
}