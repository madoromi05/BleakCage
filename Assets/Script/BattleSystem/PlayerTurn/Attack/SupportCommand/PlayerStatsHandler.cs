using UnityEngine;

/// <summary>
/// プレイヤーの主要なリソース（HP, MPなど）への変更を専門に扱うクラス。
/// PlayerModel の値を直接変更します。
/// </summary>
public class PlayerStatsHandler
{
    private readonly PlayerModel ownerModel;

    /// <summary>
    /// コンストラクタ。管理対象のPlayerModelへの参照を受け取ります。
    /// </summary>
    public PlayerStatsHandler(PlayerModel ownerModel)
    {
        this.ownerModel = ownerModel;
    }

    /// <summary>
    /// HPを回復する
    /// </summary>
    /// <param name="healAmount">回復量（フラット値）</param>
    public void ApplyHeal(float healAmount)
    {
        if (ownerModel.PlayerHP <= 0)
        {
            Debug.Log($"[PlayerStatsHandler] {ownerModel.PlayerName} は既に倒されているため、回復できません。");
            return;
        }

        if (ownerModel.PlayerHP >= ownerModel.PlayerHP)
        {
            Debug.Log($"[PlayerStatsHandler] {ownerModel.PlayerName} のHPは既に最大です。");
            return;
        }

        float oldHP = ownerModel.PlayerHP;

        // MaxHP を超えないように回復
        ownerModel.PlayerHP = Mathf.Clamp(ownerModel.PlayerHP + healAmount, 0, ownerModel.PlayerHP);

        float actualHeal = ownerModel.PlayerHP - oldHP;

        Debug.Log($"[PlayerStatsHandler] {ownerModel.PlayerName} が {actualHeal:F0} 回復。 (HP: {oldHP:F0} -> {ownerModel.PlayerHP:F0})");
    }
}