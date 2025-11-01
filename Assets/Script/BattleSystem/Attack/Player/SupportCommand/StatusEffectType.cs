/// <summary>
/// ステータス効果（バフ・デバフ）のデータを保持するクラス
/// </summary>
public enum StatusEffectType
{
    DefenceUp,
    AttackUp
    // 今後、毒、麻痺などもここに追加できる
}

public class StatusEffect
{
    public StatusEffectType Type { get; private set; }
    public float Value { get; private set; }        // 効果値 (例: 0.2 = 20%カット)
    public int DurationTurns { get; set; }  // 残り持続ターン

    public StatusEffect(StatusEffectType type, float value, int duration)
    {
        Type = type;
        Value = value;
        DurationTurns = duration;
    }
}