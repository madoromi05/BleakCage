using UnityEngine;

public class StageSelectButton : MonoBehaviour
{
    public int stageID;

    public void OnClick()
    {
        StageManager.SelectedStageID = this.stageID;
        UnityEngine.SceneManagement.SceneManager.LoadScene("Tutorial"); // バトルシーンに遷移
    }
}