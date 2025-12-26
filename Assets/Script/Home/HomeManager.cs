using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum HomeState
{
    Command,
    Option
}

public class HomeManager : MonoBehaviour
{
    [Header("UI Commands (HomeCommandButtonがついたオブジェクト)")]
    [SerializeField] HomeCommandButton[] commandButtons;

    [Header("Option UI")]
    [SerializeField] GameObject shade;
    [SerializeField] private GameObject optionPanel;
    [SerializeField] private Slider seVolumeSlider;
    [SerializeField] private Button optionCloseButton;

    private HomeState state = HomeState.Command;

    private void Start()
    {
        // UI初期化
        shade.SetActive(false);
        optionPanel.SetActive(false);
        if (seVolumeSlider != null) seVolumeSlider.gameObject.SetActive(false);
        if (optionCloseButton != null) optionCloseButton.gameObject.SetActive(false);

        // ボタンにManagerの参照を渡す
        foreach (var btn in commandButtons)
        {
            if (btn != null) btn.Setup(this);
        }
    }

    public void OnClickStory()
    {
        if (state != HomeState.Command) return;

        SoundManager.Instance.PlaySE(SEType.EnterStory);
        StageManager.SelectedStageID = 1;
        StageManager.IsPostBattle = false;
        SceneManager.LoadScene("ScenarioScene");
    }

    public void OnClickTutorial()
    {
        if (state != HomeState.Command) return;

        StageManager.SelectedStageID = 0;
        StageManager.IsPostBattle = false;
        SceneManager.LoadScene("Tutorial");
    }

    public void OnClickOption()
    {
        if (state != HomeState.Command) return;
        StartOption();
    }

    public void OnClickQuit()
    {
        if (state != HomeState.Command) return;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void StartOption()
    {
        state = HomeState.Option;
        shade.SetActive(true);
        optionPanel.SetActive(true);
        if (seVolumeSlider != null) seVolumeSlider.gameObject.SetActive(true);
        if (optionCloseButton != null) optionCloseButton.gameObject.SetActive(true);
    }

    public void FinishedOption()
    {
        state = HomeState.Command;
        shade.SetActive(false);
        optionPanel.SetActive(false);
        if (seVolumeSlider != null) seVolumeSlider.gameObject.SetActive(false);
        if (optionCloseButton != null) optionCloseButton.gameObject.SetActive(false);
    }

    public void OnSEVolumeChanged(float value)
    {
        SoundManager.Instance.SetSEVolume(value);
    }
}