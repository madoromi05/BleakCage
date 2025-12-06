using Unity.Burst.CompilerServices;
using UnityEngine;

/// <summary>
/// ステータス効果（バフ・デバフ）のデータを保持するクラス
/// </summary>
public enum StatusEffectType
{
    None,           // 【なし】: 効果なし
    DefenceUp,      // 【防御力UP】: ダメージカット
    AttackUp,       // 【攻撃力UP】: 攻撃力UP
    Fracture,       // 【破砕】: 防御貫通UP
    Laceration,     // 【損傷】: ターン終了時ダメージ
    Meltdown,       // 【熔鉄】: 攻防ダウン
    Cover,          // 【援護】: ダメージ無効化
    Target          // 【目標】: 命中率UP
}

public class StatusEffect
{
    public const int MAX_STACK = 5;               // 最大蓄積数
    public StatusEffectType Type { get; private set; }
    public float Value { get; private set; }        // 効果値
    public int DurationTurns { get; set; }          // 残り持続ターン
    public int StackCount { get; set; }             // 現在の蓄積数
    public StatusEffect(StatusEffectType type, float value, int duration, int inflictsStack)
    {
        Type = type;
        Value = value;
        DurationTurns = duration;
        StackCount = Mathf.Clamp(inflictsStack, 0, MAX_STACK);
    }

    /// <summary>
    /// スタックを追加する関数
    /// </summary>
    public void AddStack(int amount)
    {
        StackCount = Mathf.Clamp(StackCount + amount, 0, MAX_STACK);
    }
}