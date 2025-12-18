using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ScenarioSceneManager : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private Text mainText;     // Contentの中にあるText
    [SerializeField] private Button nextButton; // 画面全体を覆う透明ボタン（「次へ」用）


    [Header("【デバッグ設定】(エディタ実行時のみ有効)")]
    [SerializeField] private bool useDebugSettings = false;
    [SerializeField] private int debugStageID;
    [SerializeField] private bool debugIsPostBattle = false;// チェックを入れると「戦闘後(_Post)」、外すと「戦闘前(_Pre)
    void Start()
    {
        int stageID;
        bool isPost;
        if (Application.isEditor && useDebugSettings)
        {
            stageID = debugStageID;
            isPost = debugIsPostBattle;
            Debug.Log($"<color=yellow>デバッグモード: Stage {stageID} (Post={isPost}) を表示します</color>");
        }
        else
        {
            // 本番環境では StageManager から取得
            stageID = StageManager.SelectedStageID;
            isPost = StageManager.IsPostBattle;
        }

        LoadAndShowScenario(stageID, isPost);
        nextButton.onClick.AddListener(OnNextButtonClicked);
    }

    private void LoadAndShowScenario(int stageID, bool isPost)
    {
        string fileName;

        // チュートリアル(0)は例外処理、それ以外は Pre/Post で分岐
        if (stageID == 0)
        {
            fileName = "Scenario_0";
        }
        else
        {
            // Start(前) か End(後) かでファイル名を決定
            string suffix = isPost ? "_Start" : "_End";
            fileName = $"Scenario_{stageID}{suffix}";
        }

        string filePath = $"Scenarios/{fileName}";
        TextAsset textFile = Resources.Load<TextAsset>(filePath);

        if (textFile != null)
        {
            // 全文をそのまま流し込む（スクロールはUnityの機能で自動対応）
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
        int stageID = StageManager.SelectedStageID;
        bool isPost = StageManager.IsPostBattle;

        if (stageID == 0)
        {
            // チュートリアルの場合：シナリオが終わったらチュートリアルバトルへ
            SceneManager.LoadScene("Tutorial");
        }
        else
        {
            if (!isPost)
            {
                // 「戦闘前」シナリオが終わった → バトルへ
                SceneManager.LoadScene("BattleScene");
            }
            else
            {
                // 「戦闘後」シナリオが終わった → ステージ選択画面へ戻る
                // エンディングがあるならここで分岐
                SceneManager.LoadScene("StageSelectScene");
            }
        }
    }
}