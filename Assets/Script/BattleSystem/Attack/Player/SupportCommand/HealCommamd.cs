using UnityEngine;
using System.Collections;
/// <summary>
/// プレイヤーを回復するコマンド
/// </summary>
public class HealCommand : ICommand
{
    private readonly PlayerRuntime player;
    private readonly CardRuntime card;

    public HealCommand(PlayerRuntime player, CardRuntime card)
    {
        this.player = player;
        this.card = card;
    }

    public IEnumerator Do()
    {
        float healAmount = card.GetOutput();

        player.StatsHandler.ApplyHeal(healAmount);
        yield break;
    }

    public bool Undo()
    {
        Debug.Log("[HealCommand] Undo not implemented.");
        return false;
    }
}