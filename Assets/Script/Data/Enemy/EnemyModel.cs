using UnityEngine;

public enum EnemyAttackType
{
    Melee,  // 近距離（直接攻撃）
    Ranged  // 遠距離（魔法や飛び道具）
}

/// <summary>
/// 実行時に使用される敵キャラクターのモデルクラス
/// </summary>
public class EnemyModel
{
    public int EnemyID { get; private set; }                 // 敵のID
    public string EnemyName { get; private set; }             // 敵の名前
    public EnemyAttackType AttackType;
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
    }
}