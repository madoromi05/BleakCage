using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// プレイヤーのステータス効果（バフ・デバフ）を専門に管理するクラス。
/// PlayerRuntime ごとに1つインスタンスが生成されます。
/// </summary>
public class StatusEffectHandler
{
    private readonly PlayerModel ownerModel;

    /// <summary>
    /// 現在プレイヤーに適用されているステータス効果のリスト
    /// </summary>
    public List<StatusEffect> ActiveStatusEffects { get; private set; } = new List<StatusEffect>();

    /// <summary>
    /// コンストラクタ。所有者（PlayerModel）への参照を受け取ります。
    /// </summary>
    public StatusEffectHandler(PlayerModel ownerModel)
    {
        this.ownerModel = ownerModel;
    }

    /// <summary>
    /// このプレイヤーに新しいステータス効果を適用します
    /// </summary>
    public void ApplyBuff(StatusEffect buff)
    {
        // TODO: 同じタイプのバフが既にある場合の処理 (上書き、スタックなど)
        // 現時点では、単純に追加する
        ActiveStatusEffects.Add(buff);

        string playerName = (ownerModel != null) ? ownerModel.PlayerName : "Player";
        Debug.Log($"[{playerName}] に {buff.Type} (効果値: {buff.Value}, 持続: {buff.DurationTurns} ターン) を付与。");
    }

    /// <summary>
    /// バフのターンを1進め、期限切れのバフを削除します
    /// </summary>
    public void TickDownBuffDurations()
    {
        if (ActiveStatusEffects.Count == 0) return;

        // 逆順ループ (リストから削除を行うため)
        for (int i = ActiveStatusEffects.Count - 1; i >= 0; i--)
        {
            var buff = ActiveStatusEffects[i];
            buff.DurationTurns--;

            if (buff.DurationTurns <= 0)
            {
                string playerName = (ownerModel != null) ? ownerModel.PlayerName : "Player";
                Debug.Log($"[{playerName}] の {buff.Type} が終了。");

                ActiveStatusEffects.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 現在の防御バフによる補正値（ダメージカット率など）を取得します
    /// </summary>
    public float GetDefenseModifier()
    {
        float totalModifier = 0f;
        foreach (var buff in ActiveStatusEffects)
        {
            if (buff.Type == StatusEffectType.DefenceUp)
            {
                // 防御バフが複数ある場合は、効果値を合計する (仕様による)
                totalModifier += buff.Value;
            }
        }
        return totalModifier;
    }
}