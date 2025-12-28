using System;
using UnityEngine;

public class EnemyHpHandler
{
    private readonly EnemyRuntime _ownerRuntime;

    public event Action<EnemyRuntime> OnDead;
    public event Action<float, float> OnHpChanged;

    public EnemyHpHandler(EnemyRuntime ownerRuntime)
    {
        _ownerRuntime = ownerRuntime;
    }

    public void TakeDamage(float damage)
    {
        if (_ownerRuntime == null) return;

        if (_ownerRuntime.CurrentHP <= 0f) return;

        // 【援護(Cover)】チェック（StatusHandler が null の可能性があるならガード）
        if (_ownerRuntime.StatusHandler != null &&
            _ownerRuntime.StatusHandler.GetStackCount(StatusEffectType.Cover) > 0)
        {
            _ownerRuntime.StatusHandler.ConsumeStack(StatusEffectType.Cover, 1);
            return;
        }

        float oldHp = _ownerRuntime.CurrentHP;
        _ownerRuntime.CurrentHP = Mathf.Max(0f, _ownerRuntime.CurrentHP - damage);
        float maxHp = _ownerRuntime.MaxHP;

        OnHpChanged?.Invoke(_ownerRuntime.CurrentHP, maxHp);

        if (oldHp > 0f && _ownerRuntime.CurrentHP <= 0f)
        {
            OnDead?.Invoke(_ownerRuntime);
        }
    }

    public void Heal(float amount)
    {
        if (_ownerRuntime == null) return;
        if (_ownerRuntime.CurrentHP <= 0f) return;

        float maxHp = _ownerRuntime.MaxHP;

        _ownerRuntime.CurrentHP = Mathf.Min(maxHp, _ownerRuntime.CurrentHP + amount);
        OnHpChanged?.Invoke(_ownerRuntime.CurrentHP, maxHp);
    }
}
