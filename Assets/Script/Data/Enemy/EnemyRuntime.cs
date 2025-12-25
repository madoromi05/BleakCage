using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敵の実体（Runtime）クラス
/// HP管理、ステータス異常管理を行う
/// </summary>
public class EnemyRuntime
{
    public int ID { get; private set; }
    public Guid InstanceID { get; private set; }
    public float CurrentHP { get; set; }
    public float MaxHP => EnemyModel.MaxHP;
    public EnemyModel EnemyModel { get; private set; }
    public StatusEffectHandler StatusHandler { get; private set; }
    public EnemyHPHandler HPHandler { get; private set; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public EnemyRuntime(EnemyModel model, string instanceID)
    {
        this.EnemyModel = model;
        this.ID = model.EnemyID;
        this.InstanceID = Guid.Parse(instanceID);
        this.CurrentHP = model.EnemyHP;
        this.StatusHandler = new StatusEffectHandler(model.EnemyName);
        this.HPHandler = new EnemyHPHandler(this);
    }

    /// <summary>
    /// 現在の攻撃力を取得（【熔鉄】などの補正込み）
    /// </summary>
    public float GetCurrentAttackPower()
    {
        float basePower = EnemyModel.EnemyAttackPower;
        float multiplier = 1.0f;

        // 【熔鉄】チェック: 1スタックにつき10%ダウン
        if (StatusHandler != null)
        {
            int meltdown = StatusHandler.GetStackCount(StatusEffectType.Meltdown);
            if (meltdown > 0)
            {
                multiplier -= (0.10f * meltdown);
            }
        }

        // 0未満にならないようにする
        return Mathf.Max(0, basePower * multiplier);
    }

    /// <summary>
    /// 現在の防御力を取得（【熔鉄】などの補正込み）
    /// </summary>
    public float GetCurrentDefensePower()
    {
        float baseDefense = EnemyModel.EnemyDefensePower;
        float multiplier = 1.0f;

        // 【熔鉄】チェック: 1スタックにつき5%ダウン
        if (StatusHandler != null)
        {
            int meltdown = StatusHandler.GetStackCount(StatusEffectType.Meltdown);
            if (meltdown > 0)
            {
                multiplier -= (0.05f * meltdown);
            }
        }

        return Mathf.Max(0, baseDefense * multiplier);
    }
}