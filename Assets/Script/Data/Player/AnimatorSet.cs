using UnityEngine;

/// <summary>
/// プレイヤーの基本的なアニメーションクリップをまとめるデータセット。
/// ScriptableObjectとして、キャラクターごとにアセットファイルを作成して使用します。
/// </summary>
[CreateAssetMenu(fileName = "AnimationSet", menuName = "Animation Set")]
public class AnimatorSet : ScriptableObject
{
    [Header("待機アニメーション")]
    public AnimationClip Idle;

    //[Header("死亡アニメーション")]
    //public AnimationClip Death;

    //[Header("ダメージ受けアニメーション")]
    //public AnimationClip Damaged;

    [Header("防御アニメーション")]
    public AnimationClip Guard;
}
