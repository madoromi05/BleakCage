using UnityEngine;

/// <summary>
/// プレイヤーの基本的なアニメーションクリップをまとめるデータセット。
/// ScriptableObjectとして、キャラクターごとにアセットファイルを作成して使用します。
/// </summary>
[CreateAssetMenu(fileName = "PlayerAnimationSet", menuName = "Player Animation Set")]
public class AnimatorSet : ScriptableObject
{
    [Header("アバター")]
    public Avatar avatar;

    [Header("待機アニメーション")]
    public AnimationClip Idle;

    [Header("死亡アニメーション")]
    public AnimationClip Death;

    [Header("ダメージ受けアニメーション")]
    public AnimationClip Damaged;
}