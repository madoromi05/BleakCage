using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ‰¹ºŠÇ—•Ä¶
/// </summary>
public enum SEType
{
    //System
    Check,
    Cancel,
    SwitchingPhases,
    CountDown,

    checkedSkillCard,
    startedSelectCard,

    //PlayerAttack
    BulletAttack,
    PierceAttack,
    BluntAttack,
    SlashAttack,
    //damagedPlayer
    damagedPlayer,
    //SpecialSkill
    Defence,
    Heal,
    //damagedEnemy SE‚©‚ç‚±‚¤‚µ‚½‚ªË—Í‚Æ‚©‚Ì‚Æê’ê—‚ª”­¶‚·‚é‚Ì‚Å•Û—¯
    damagedBulletEnemy,
    damagedPierceEnemy,
    damagedBluntEnemy,
    damagedSlashEnemy,
}
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    public List<SoundData> seList;
    private Dictionary<SEType, AudioClip> seDict;
    private AudioSource audioSource;

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
    }

    public void PlaySE(SEType type)
    {
        if (seDict.TryGetValue(type, out var clip))
            audioSource.PlayOneShot(clip);
    }
}
