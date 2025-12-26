using UnityEngine;

/// <summary>
/// 選択されたステージIDをシーン間で共有するための静的クラス
/// </summary>
public static class StageManager
{
    public static int SelectedStageID { get; set; } =-1;
    public static bool IsPostBattle = false;

    private const string PREF_KEY_MAX_STAGE = "MaxReachedStage";

    /// <summary>
    /// プレイヤーが到達した最大のステージIDを取得する
    /// (データがない場合は 1 を返す)
    /// </summary>
    public static int GetMaxReachedStage()
    {
        return PlayerPrefs.GetInt(PREF_KEY_MAX_STAGE, 1);
    }

    /// <summary>
    /// ステージをクリアした時に呼び出す。
    /// 次のステージを解放する（セーブする）。
    /// </summary>
    /// <param name="clearedStageID">クリアしたステージID</param>
    public static void OnStageCleared(int clearedStageID)
    {
        int currentMax = GetMaxReachedStage();
        int nextStage = clearedStageID + 1;

        // もし新しいステージに進んだなら、セーブデータを更新する
        if (nextStage > currentMax)
        {
            PlayerPrefs.SetInt(PREF_KEY_MAX_STAGE, nextStage);
            PlayerPrefs.Save();
            Debug.Log($"セーブ完了: Stage {nextStage} 解放");
        }
    }

    /// <summary>
    /// デバッグ用: セーブデータをリセットする
    /// </summary>
    public static void ResetProgress()
    {
        PlayerPrefs.DeleteKey(PREF_KEY_MAX_STAGE);
    }
}