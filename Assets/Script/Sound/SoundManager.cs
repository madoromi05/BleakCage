using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    public List<BGMData> bgmList;
    private Dictionary<SEType, AudioClip> seDict;
    private Dictionary<BGMType, BGMData> bgmDataDict;
    private AudioSource seSource;
    private AudioSource bgmSource;

    private float seMasterVolume = 0.5f;
    private float bgmMasterVolume = 0.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // --- SE初期化 ---
        seSource = gameObject.AddComponent<AudioSource>();
        seDict = new Dictionary<SEType, AudioClip>();
        foreach (var data in seList) seDict[data.type] = data.clip;

        // BGM初期化
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true; // ループ再生
        bgmDataDict = new Dictionary<BGMType, BGMData>();
        foreach (var data in bgmList) bgmDataDict[data.type] = data;

        SetSEVolume(0.5f);
        SetBGMVolume(0.5f);
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        switch (newScene.name)
        {
            case "HomeScene":
            case "ScenarioScene":
            case "DeckViewScene":
                PlayBGM(BGMType.Title);
                break;

            case "BattleScene":
                // StageID確定後に BattleEntitiesManager が上書きするので、ここは無音でOK
                PlayBGM(BGMType.None);
                break;
        }
    }
    public void PlaySE(SEType type)
    {
        if (seDict.TryGetValue(type, out var clip))
            seSource.PlayOneShot(clip, seMasterVolume);
    }

    public void PlayBGM(BGMType type)
    {
        // 定義がない、またはNoneなら停止
        if (type == BGMType.None || !bgmDataDict.TryGetValue(type, out BGMData data))
        {
            StopBGM();
            return;
        }

        if (bgmSource.isPlaying && bgmSource.clip == data.clip) return;

        bgmSource.clip = data.clip;
        bgmSource.volume = bgmMasterVolume * data.volumeScale;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        bgmSource.Stop();
        bgmSource.clip = null;
    }

    public void SetSEVolume(float volume)
    {
        seMasterVolume = Mathf.Clamp01(volume);
        seSource.volume = seMasterVolume;
    }

    public void SetBGMVolume(float volume)
    {
        bgmMasterVolume = Mathf.Clamp01(volume);
        if (bgmSource != null)
        {
            float scale = 1f;
            if (bgmSource.clip != null)
            {
                foreach (var kvp in bgmDataDict)
                {
                    if (kvp.Value.clip == bgmSource.clip)
                    {
                        scale = kvp.Value.volumeScale;
                        break;
                    }
                }
            }
            bgmSource.volume = bgmMasterVolume * scale;
        }
    }
}
