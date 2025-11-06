/// <summary>
/// プレイヤーの防御行動の結果
/// </summary>
public enum DefenseResult
{
    None,   // 何もしなかった (被弾)
    Guard,  // ガード成功
    Counter // カウンター成功 (ジャストガード)
}