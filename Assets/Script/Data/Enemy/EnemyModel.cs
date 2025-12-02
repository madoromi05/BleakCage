using UnityEngine;

/// <summary>
/// 実行時に使用される敵キャラクターのモデルクラス
/// </summary>
public class EnemyModel
{
    public int EnemyID { get; private set; }                 // 敵のID
    public string EnemyName { get; private set; }             // 敵の名前
    public float EnemyHP { get; set; }                       // 敵のHP
    public float MaxHP { get; private set; }
    public float EnemyAttackPower { get; private set; }      // 攻撃力
    public float EnemyDefensePower { get; private set; }     // 防御力
    public AttributeType EnemyAttribute { get; private set; } // 攻撃属性
    public DefensAttributeType EnemyDefensAttribute { get; private set; } // 防御属性
    public Sprite EnemySprite { get; private set; }           // 表示アイコン
    public string EnemyDescription { get; private set; }      // 説明文
    public EnemyAnimatorSet EnemyAnimator { get; private set; }   // アニメーションセット
    public GameObject CharacterPrefab { get; private set; }
    public Vector3 InitialRotation { get; private set; }   // 初期回転
    public StatusEffectHandler StatusHandler { get; private set; }
    public EnemyHPHandler HPHandler { get; private set; }

    /// <summary>
    /// ScriptableObject(EnemyEntity)からデータを読み込んでモデルに反映
    /// </summary>
    public EnemyModel(EnemyEntity Entity)
    {
        if (Entity == null)
        {
            Debug.LogError("enemyEntity is null");
            return;
        }
        EnemyID = Entity.EnemyID;
        EnemyName = Entity.EnemyName;
        EnemyHP = Entity.EnemyHP;
        MaxHP = Entity.EnemyHP;
        EnemyAttackPower = Entity.EnemyAttackPower;
        EnemyDefensePower = Entity.EnemyDefensePower;
        EnemyAttribute = Entity.EnemyAttribute;
        EnemyDefensAttribute = Entity.EnemyDefensAttribute;
        EnemySprite = Entity.EnemySprite;
        EnemyDescription = Entity.EnemyDescription;
        EnemyAnimator = Entity.AnimationSet;

        CharacterPrefab = Entity.CharacterPrefab;
        InitialRotation = Entity.InitialRotation;
        StatusHandler = new StatusEffectHandler(EnemyName);
        HPHandler = new EnemyHPHandler(this);
    }

    // <summary>
    /// 現在の攻撃力を取得（【熔鉄】などの補正込み）
    /// </summary>
    public float GetCurrentAttackPower()
    {
        float multiplier = 1.0f;

        // 【熔鉄】チェック: 1スタックにつき10%ダウン
        int meltdown = StatusHandler.GetStackCount(StatusEffectType.Meltdown);
        if (meltdown > 0)
        {
            multiplier -= (0.10f * meltdown);
        }

        // 0未満にならないようにする
        return Mathf.Max(0, EnemyAttackPower * multiplier);
    }

    /// <summary>
    /// 現在の防御力を取得（【熔鉄】などの補正込み）
    /// </summary>
    public float GetCurrentDefensePower()
    {
        float multiplier = 1.0f;

        // 【熔鉄】チェック: 1スタックにつき5%ダウン
        int meltdown = StatusHandler.GetStackCount(StatusEffectType.Meltdown);
        if (meltdown > 0)
        {
            multiplier -= (0.05f * meltdown);
        }

        return Mathf.Max(0, EnemyDefensePower * multiplier);
    }
}