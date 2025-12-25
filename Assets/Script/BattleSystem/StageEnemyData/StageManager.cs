/// <summary>
/// 選択されたステージIDをシーン間で共有するための静的クラス
/// </summary>
public static class StageManager
{
    public static int SelectedStageID { get; set; } =-1;
    public static bool IsPostBattle = false;
}