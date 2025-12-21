using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ホーム画面
/// </summary>
enum HomeState
{
    Command,
    Option
}

enum CommandSelect
{
    Story,
    Tutorial,
    Option,
    Quit
}

public class HomeManager : MonoBehaviour
{
    [Header("UI Commands（enum順に並べる）")]
    [SerializeField] GameObject[] selectCommands;

    [Header("UI Position")]
    [SerializeField] Vector2 normalPos = Vector2.zero;
    [SerializeField] Vector2 selectedPos = new Vector2(10f, 0f); [SerializeField] float selectOffsetX = 20f;
    private Vector2[] basePositions;

    private HomeState state = HomeState.Command;

    private int selectIndex = 0;
    private CommandSelect current;
    [SerializeField] GameObject shade;
    [SerializeField] private GameObject optionPanel;
    [SerializeField] private UnityEngine.UI.Slider seVolumeSlider;

    private void Start()
    {
        shade.SetActive(false);
        optionPanel.SetActive(false);

        seVolumeSlider.value = SoundManager.Instance.GetSEVolume(); 
        seVolumeSlider.gameObject.SetActive(false);


        if (selectCommands == null || selectCommands.Length == 0)
        {
            Debug.LogError("selectCommands が設定されていません");
            enabled = false;
            return;
        }

        basePositions = new Vector2[selectCommands.Length];

        for(int i = 0; i < selectCommands.Length; i++)
        {
            RectTransform rt = selectCommands[i].GetComponent<RectTransform>();
            basePositions[i] = rt.anchoredPosition;
        }

        ChangeSelect(0);
    }

    private void Update()
    {
        switch (state)
        {
            case HomeState.Command:
                HandleInput();
                break;

            case HomeState.Option:
                Option();
                break;
        }
    }

    private void HandleInput()
    {
        if(Input.GetKeyDown(KeyCode.UpArrow)) ChangeSelect(selectIndex - 1);

        if(Input.GetKeyDown(KeyCode.DownArrow)) ChangeSelect(selectIndex + 1);

        if(Input.GetKeyDown(KeyCode.Return)) Decide();
    }

    private void ChangeSelect(int newIndex)
    {
        newIndex = Mathf.Clamp(newIndex, 0, selectCommands.Length - 1);
        if (selectIndex == newIndex) return;

        selectIndex = newIndex;
        current = (CommandSelect)selectIndex;

        UpdateView();
    }
    private void UpdateView()
    {
        for(int i = 0; i < selectCommands.Length; i++)
        {
            RectTransform rt = selectCommands[i].GetComponent<RectTransform>();
            if(rt == null) continue;

            Vector2 pos = basePositions[i];

            if(i == selectIndex)
                pos.x += selectOffsetX;

            rt.anchoredPosition = pos;
        }
    }
    public void OnSEVolumeChanged(float value)
    {
        SoundManager.Instance.SetSEVolume(value);
    }

    private void StartOption()
    {
        state = HomeState.Option;

        shade.SetActive(true);
        optionPanel.SetActive(true);
        seVolumeSlider.gameObject.SetActive(true);

        foreach (var cmd in selectCommands)
            cmd.SetActive(false);

        seVolumeSlider.value = SoundManager.Instance.GetSEVolume();
    }

    private void Option()
    {

        if (Input.GetKeyDown(KeyCode.Return))
        {
            FinishedOption();
        } 
    }
    private void FinishedOption()
    {
        state = HomeState.Command;

        shade.SetActive(false);
        optionPanel.SetActive(false);
        seVolumeSlider.gameObject.SetActive(false);

        foreach(var cmd in selectCommands) cmd.SetActive(true);

        UpdateView();
    }

    private void Decide()
    {
        switch(current)
        {
            case CommandSelect.Story:
                SoundManager.Instance.PlaySE(SEType.EnterStory);
                SceneManager.LoadScene("DeckEditing");
                break;

            case CommandSelect.Tutorial:
                SceneManager.LoadScene("Tutorial");
                break;

            case CommandSelect.Option:
                StartOption();
                break;

            case CommandSelect.Quit:
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                break;
        }
    }
}
