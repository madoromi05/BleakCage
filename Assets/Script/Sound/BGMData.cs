using UnityEngine;

/// <summary>
/// BGMの種類（シンプル版）
/// </summary>
public enum BGMType
{
    None,           // 無音
    Title,          // タイトル・ホーム・シナリオ共通
    Battle_Normal,  // 通常戦闘 (チュートリアル含む)
    Battle_Boss     // ボス戦
}

[System.Serializable]
public class BGMData
{
    public BGMType type;
    public AudioClip clip;
    [Range(0f, 1f)] public float volumeScale = 1f;
}