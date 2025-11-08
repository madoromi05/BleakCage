using System.Collections.Generic;
using System.Linq;
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
    public List<AnimationClip> AttackAnimations = new List<AnimationClip>();

    /// <summary>
    /// 設定されている攻撃アニメーションからランダムに1つ取得します。
    /// </summary>
    /// <returns>ランダムに選ばれたAnimationClip。リストが空の場合はnull。</returns>
    public AnimationClip GetRandomAttackClip()
    {
        // リストが空か、null でないかを確認
        if (AttackAnimations == null || !AttackAnimations.Any())
        {
            Debug.LogWarning($"EnemyAnimatorSet ({this.name}): 攻撃アニメーションが1つも設定されていません。");
            return null;
        }

        // リストの範囲内でランダムなインデックスを生成
        int index = Random.Range(0, AttackAnimations.Count);
        return AttackAnimations[index];
    }

    /// <summary>
    /// インデックスを指定して攻撃アニメーションを取得します。
    /// </summary>
    public AnimationClip GetAttackClipByIndex(int index)
    {
        if (AttackAnimations == null || index < 0 || index >= AttackAnimations.Count)
        {
            Debug.LogWarning($"EnemyAnimatorSet ({this.name}): 範囲外のインデックス {index} が要求されました。");
            return null;
        }
        return AttackAnimations[index];
    }
}