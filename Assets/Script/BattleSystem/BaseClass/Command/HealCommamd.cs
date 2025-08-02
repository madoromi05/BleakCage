using UnityEngine;

public class HealCardCommand : ICardCommand
{
    private readonly PlayerModel player;
    private readonly float healRatio;  // 0.1f など
    private readonly float flatHeal;   // 回復値が固定の場合はこちらを使用
    private readonly bool useRatio;    // 割合か固定か

    public HealCardCommand(PlayerModel player, float value, bool useRatio = false)
    {
        this.player = player;
        this.useRatio = useRatio;
        if (useRatio)
            this.healRatio = value;
        else
            this.flatHeal = value;
    }

    public bool Do()
    {
        float healAmount = useRatio ? player.PlayerHP * healRatio : flatHeal;

        // HPを回復。ただし上限チェックが必要なら別途。
        player.PlayerHP += healAmount;
        Debug.Log($"[HealCardCommand] {healAmount} 回復。現在HP: {player.PlayerHP}");

        return true;
    }

    public bool Undo()
    {
        Debug.Log("[HealCardCommand] Undo not implemented.");
        return false;
    }
}
