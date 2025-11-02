using UnityEngine;
using UnityEngine.SceneManagement;

public class StageSelectButton : MonoBehaviour
{
    public int stageID;

    public void OnClick()
    {
        // ステージIDを保存
        StageManager.SelectedStageID = this.stageID;

        // チュートリアル or 通常バトルを判定
        if (stageID == 0)
        {
            SceneManager.LoadScene("Tutorial");
        }
        else
        {
            SceneManager.LoadScene("BattleScene");
        }
    }
}
