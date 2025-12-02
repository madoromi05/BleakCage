using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// プレイヤー、敵のステータス効果（バフ・デバフ）を専門に管理するクラス。
/// PlayerRuntime ごとに1つインスタンスが生成されます。
/// </summary>
public class StatusEffectHandler
{
    private readonly string ownerName;

    /// <summary>
    /// 現在プレイヤーに適用されているステータス効果のリスト
    /// </summary>
    public List<StatusEffect> ActiveStatusEffects { get; private set; } = new List<StatusEffect>();

    /// <summary>
    /// コンストラクタ。所有者（PlayerModel）への参照を受け取ります。
    /// </summary>
    public StatusEffectHandler(string name)
    {
        this.ownerName = name;
    }

    /// <summary>
    /// このプレイヤーに新しいステータス効果を適用します
    /// </summary>
    public void ApplyStatus(StatusEffect newEffect)
    {
        var existing = ActiveStatusEffects.FirstOrDefault(e => e.Type == newEffect.Type);

        if (existing != null)
        {
            // 既に存在する場合はスタックを加算し、効果時間を延長
            existing.AddStack(newEffect.StackCount);
            existing.DurationTurns = Mathf.Max(existing.DurationTurns, newEffect.DurationTurns);
            Debug.Log($"[{ownerName}] {newEffect.Type} Stack Updated: {existing.StackCount}");
        }
        else
        {
            ActiveStatusEffects.Add(newEffect);
            Debug.Log($"[{ownerName}] {newEffect.Type} Applied. Stack: {newEffect.StackCount}");
        }
    }

    /// <summary>
    /// 指定したタイプのスタック数を取得
    /// </summary>
    public int GetStackCount(StatusEffectType type)
    {
        var effect = ActiveStatusEffects.FirstOrDefault(e => e.Type == type);
        return effect != null ? effect.StackCount : 0;
    }

    /// <summary>
    /// スタックを消費・削除する（【援護】などで使用）
    /// </summary>
    public void ConsumeStack(StatusEffectType type, int amount = 1)
    {
        var effect = ActiveStatusEffects.FirstOrDefault(e => e.Type == type);
        if (effect != null)
        {
            effect.StackCount -= amount;
            Debug.Log($"[{ownerName}] {type} Stack Consumed. Remaining: {effect.StackCount}");

            if (effect.StackCount <= 0)
            {
                ActiveStatusEffects.Remove(effect);
                Debug.Log($"[{ownerName}] {type} Removed.");
            }
        }
    }

    /// <summary>
    /// ターン経過処理
    /// </summary>
    public void TickDownBuffDurations()
    {
        for (int i = ActiveStatusEffects.Count - 1; i >= 0; i--)
        {
            var buff = ActiveStatusEffects[i];
            buff.DurationTurns--;
            if (buff.DurationTurns <= 0)
            {
                ActiveStatusEffects.RemoveAt(i);
                Debug.Log($"[{ownerName}] {buff.Type} Expired.");
            }
        }
    }
}