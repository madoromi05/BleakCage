using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ScenarioSceneManager : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private Text mainText;      // Contentの中にあるText
    [SerializeField] private Button nextButton; // 画面全体を覆う透明ボタン（「次へ」用）

    [Header("【デバッグ設定】(エディタ実行時のみ有効)")]
    [SerializeField] private bool useDebugSettings = false;
    [SerializeField] private int debugStageID;
    [SerializeField] private bool debugIsPostBattle = false;

    // 現在の状態を保持する変数
    private int currentStageID;
    private bool currentIsPost;

    void Start()
    {
        if (Application.isEditor && useDebugSettings)
        {
            currentStageID = debugStageID;
            currentIsPost = debugIsPostBattle;
            Debug.Log($"<color=yellow>デバッグモード: Stage {currentStageID} (Post={currentIsPost}) を表示します</color>");
        }
        else
        {
            // 本番環境では StageManager から取得
            currentStageID = StageManager.SelectedStageID;
            currentIsPost = StageManager.IsPostBattle;
        }

        if (currentStageID == 0 || (currentStageID == 1 && currentIsPost))
        {
            NavigateToNextScene();
            return;
        }

        // シナリオのロードと表示
        LoadAndShowScenario(currentStageID, currentIsPost);
        nextButton.onClick.AddListener(OnNextButtonClicked);
    }

    private void LoadAndShowScenario(int stageID, bool isPost)
    {
        string suffix = isPost ? "_End" : "_Start";
        string fileName = $"Scenario_{stageID}{suffix}";

        string filePath = $"Scenarios/{fileName}";
        TextAsset textFile = Resources.Load<TextAsset>(filePath);

        if (textFile != null)
        {
            // 全文をそのまま流し込む
            mainText.text = textFile.text;
        }
        else
        {
            string errorMsg = $"シナリオファイルが見つかりません: {fileName}";
            Debug.LogWarning(errorMsg);
            mainText.text = errorMsg;
        }
    }

    public void OnNextButtonClicked()
    {
        NavigateToNextScene();
    }

    private void NavigateToNextScene()
    {
        // メンバ変数に保存した値を使用
        if (!currentIsPost)
        {
            // 「戦闘前」だったので、次は「バトル」へ
            SceneManager.LoadScene("BattleScene");
        }
        else
        {
            // 「戦闘後」だったので、次は「ステージ選択（またはエンディング）」へ
            SceneManager.LoadScene("StageSelectScene");
        }
    }
}