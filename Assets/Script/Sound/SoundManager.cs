using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// âπê∫ä«óùÅïçƒê∂
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
    //damagedEnemy
    damagedBulletEnemy,
    damagedPierceEnemy,
    damagedBluntEnemy,
    damagedSlashEnemy,

    EnterStory,
    MetalExplosion,
    GunpowderExplosion,
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
    /// BGMÇÃVolumeí≤êÆ
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
