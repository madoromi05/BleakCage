using UnityEngine;

/// <summary>
/// 選択されたステージIDをシーン間で共有するための静的クラス
/// </summary>
public static class StageManager
{
    public static int SelectedStageID { get; set; } = -1;
    public static bool IsPostBattle = false;

    private const string PREF_KEY_MAX_STAGE = "MaxReachedStage";
    private const int MIN_STAGE = 1;
    private const int MAX_STAGE = 3;


    /// <summary>
    /// プレイヤーが到達した最大のステージIDを取得する
    /// (データがない場合は 1 を返す)
    /// </summary>
    public static int GetMaxReachedStage()
    {
        return PlayerPrefs.GetInt(PREF_KEY_MAX_STAGE, MIN_STAGE);
    }

    /// <summary>
    /// ステージクリア時に呼ばれる
    /// </summary>
    public static void OnStageCleared(int clearedStageID)
    {
        int nextStage = GetNextStageID(clearedStageID);

        PlayerPrefs.SetInt(PREF_KEY_MAX_STAGE, nextStage);
        PlayerPrefs.Save();

        DebugCostom.Log($"セーブ完了: 次のStage = {nextStage}");
    }

    /// <summary>
    /// 次に進むべきステージIDを返す
    /// MAX_STAGE を超えたら MIN_STAGE に戻す
    /// </summary>
    public static int GetNextStageID(int currentStageID)
    {
        if (currentStageID >= MAX_STAGE)
        {
            return MIN_STAGE;
        }
        return currentStageID + 1;
    }
}