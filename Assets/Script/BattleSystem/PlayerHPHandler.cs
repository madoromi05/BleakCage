using UnityEngine;
using System; // Actionのために追加

public class PlayerHPHandler
{
    private readonly PlayerRuntime ownerRuntime;

    // 死亡時に通知するイベント
    public event Action<PlayerRuntime> OnDead;

    public PlayerHPHandler(PlayerRuntime ownerRuntime)
    {
        this.ownerRuntime = ownerRuntime;
    }

    public void ApplyHeal(float healAmount)
    {
        float current = ownerRuntime.CurrentHP;
        float max = ownerRuntime.MaxHP;

        if (current <= 0 || current >= max) return; // 死亡している場合は回復しない等のルールがあればここ

        float oldHP = current;
        ownerRuntime.CurrentHP = Mathf.Clamp(current + healAmount, 0, max);

        Debug.Log($"[{ownerRuntime.PlayerModel.PlayerName}] Healed: {ownerRuntime.CurrentHP - oldHP}");
    }

    public void TakeDamage(float damage)
    {
        if (ownerRuntime.CurrentHP <= 0) return;

        float oldHP = ownerRuntime.CurrentHP;
        ownerRuntime.CurrentHP = Mathf.Clamp(ownerRuntime.CurrentHP - damage, 0, ownerRuntime.MaxHP);

        Debug.Log($"[{ownerRuntime.PlayerModel.PlayerName}] Damaged: {damage} (HP: {oldHP} -> {ownerRuntime.CurrentHP})");

        // 死亡判定
        if (ownerRuntime.CurrentHP <= 0)
        {
            OnDead?.Invoke(ownerRuntime);
        }
    }
}