using UnityEngine;
using System;

public class PlayerHPHandler
{
    public event Action<PlayerRuntime> OnDead;
    public event Action<float, float> OnHPChanged;

    private readonly PlayerRuntime _ownerRuntime;
    public PlayerHPHandler(PlayerRuntime ownerRuntime)
    {
        _ownerRuntime = ownerRuntime;
    }

    public void Heal(float amount)
    {
        _ownerRuntime.CurrentHP = Mathf.Min(_ownerRuntime.CurrentHP + amount, _ownerRuntime.MaxHP);
        OnHPChanged?.Invoke(_ownerRuntime.CurrentHP, _ownerRuntime.MaxHP);
    }

    public void TakeDamage(float damage)
    {
        if (_ownerRuntime.CurrentHP <= 0) return;

        float oldHP = _ownerRuntime.CurrentHP;
        _ownerRuntime.CurrentHP = Mathf.Clamp(_ownerRuntime.CurrentHP - damage, 0, _ownerRuntime.MaxHP);
        Debug.Log($"[{_ownerRuntime.PlayerModel.PlayerName}] Damaged: {damage} (HP: {oldHP} -> {_ownerRuntime.CurrentHP})");
        OnHPChanged?.Invoke(_ownerRuntime.CurrentHP, _ownerRuntime.MaxHP);

        // Ž€–S”»’è
        if (_ownerRuntime.CurrentHP <= 0)
        {
            OnDead?.Invoke(_ownerRuntime);
        }
    }
}