using UnityEngine;

/// <summary>
/// プレイヤーの基本的なアニメーションクリップをまとめるデータセット。
/// ScriptableObjectとして、キャラクターごとにアセットファイルを作成して使用します。
/// </summary>
[CreateAssetMenu(fileName = "EnemyAnimationSet", menuName = "EnemyAnimation Set")]
public class EnemyAnimatorSet : ScriptableObject
{
    [Header("待機アニメーション")]
    public AnimationClip Idle;

    [Header("攻撃アニメーション")]
    public AnimationClip Attack001;
    public AnimationClip Attack002;
    public AnimationClip Attack003;
}