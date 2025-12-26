using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 効果音（SE）の種類定義
/// </summary>
public enum SEType
{
    // --- システム ---
    Check,              // 決定
    Cancel,             // キャンセル
    SwitchingPhases,    // フェーズ切替
    CountDown,          // 制限時間カウントダウン

    // --- カード選択 ---
    checkedSkillCard,   // スキルカード選択・決定
    startedSelectCard,  // 選択フェーズ開始

    // --- 攻撃アクション ---
    BulletAttack,       // 攻撃：銃撃（弾属性）
    PierceAttack,       // 攻撃：刺突（突属性）
    BluntAttack,        // 攻撃：打撃（鈍属性）
    SlashAttack,        // 攻撃：斬撃（斬属性）

    // --- リアクション・スキル ---
    damagedPlayer,      // 被ダメージ：プレイヤー
    Defence,            // 防御スキル / 汎用バフ
    Heal,               // 回復スキル

    // --- 敵の被ダメージ ---
    damagedBulletEnemy, // 被ダメージ：敵（弾）
    damagedPierceEnemy, // 被ダメージ：敵（突）
    damagedBluntEnemy,  // 被ダメージ：敵（鈍）
    damagedSlashEnemy,  // 被ダメージ：敵（斬）

    // --- シナリオ・演出 ---
    EnterStory,         // 物語パート開始
    //MetalExplosion,     // 爆発（金属系）
    //GunpowderExplosion, // 爆発（火薬系）
}
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    public List<SoundData> seList;
    private Dictionary<SEType, AudioClip> seDict;
    private AudioSource audioSource;

    private float seVolume = 1f;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        audioSource = GetComponent<AudioSource>();
        seDict = new();
        foreach (var data in seList)  seDict[data.type] = data.clip;
        audioSource.volume = seVolume;
    }

    public void PlaySE(SEType type)
    {
        if (seDict.TryGetValue(type, out var clip))
            audioSource.PlayOneShot(clip, seVolume);
    }
    /// <summary>
    /// BGMのVolume調整
    /// </summary>
    public void SetSEVolume(float volume)
    {
        audioSource.volume = Mathf.Clamp01(volume);
        audioSource.volume = seVolume;
    }
    public float GetSEVolume()
    {
        return seVolume;
    }
}
