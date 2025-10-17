// IPhase.cs
public interface IPhase
{
    // フェーズを開始する
    void StartPhase();
    // フェーズが完了したことを通知するイベント
    event System.Action OnPhaseFinished;
}