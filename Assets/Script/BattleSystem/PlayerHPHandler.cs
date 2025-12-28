using System;
using UnityEngine;

public class PlayerHpHandler
{
    private readonly PlayerRuntime _ownerRuntime;

    public event Action<PlayerRuntime> OnDead;
    public event Action<float, float> OnHpChanged;

    public PlayerHpHandler(PlayerRuntime ownerRuntime)
    {
        _ownerRuntime = ownerRuntime;
    }

    public void Heal(float amount)
    {
        if (_ownerRuntime == null) return;
        if (_ownerRuntime.CurrentHP <= 0f) return;

        _ownerRuntime.CurrentHP = Mathf.Min(_ownerRuntime.CurrentHP + amount, _ownerRuntime.MaxHP);
        OnHpChanged?.Invoke(_ownerRuntime.CurrentHP, _ownerRuntime.MaxHP);
    }

    public void TakeDamage(float damage)
    {
        if (_ownerRuntime == null) return;
        if (_ownerRuntime.CurrentHP <= 0f) return;

        float oldHp = _ownerRuntime.CurrentHP;
        _ownerRuntime.CurrentHP = Mathf.Clamp(_ownerRuntime.CurrentHP - damage, 0f, _ownerRuntime.MaxHP);

        OnHpChanged?.Invoke(_ownerRuntime.CurrentHP, _ownerRuntime.MaxHP);

        if (oldHp > 0f && _ownerRuntime.CurrentHP <= 0f)
        {
            OnDead?.Invoke(_ownerRuntime);
        }
    }
}
