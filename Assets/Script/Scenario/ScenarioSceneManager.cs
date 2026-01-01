using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// ステージIDと戦闘前後フラグに基づき、シナリオのロードとシーン遷移を管理するクラス
/// </summary>
public class ScenarioSceneManager : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private Text _mainText;
    [SerializeField] private Button _nextButton;

    [Header("【デバッグ設定】(エディタ実行時のみ有効)")]
    [SerializeField] private bool _useDebugSettings = false;
    [SerializeField] private int _debugStageID;
    [SerializeField] private bool _debugIsPostBattle = false;

    private int _currentStageID;
    private bool _currentIsPost;

    private void Start()
    {
        if (StageManager.SelectedStageID != -1)
        {
            _currentStageID = StageManager.SelectedStageID;
            _currentIsPost = StageManager.IsPostBattle;
        }
        else if (Application.isEditor && _useDebugSettings)
        {
            // エディタ実行時かつデバッグ設定が有効な場合Editor状のIDを参照
            _currentStageID = _debugStageID;
            _currentIsPost = _debugIsPostBattle;
            DebugCostom.Log($"<color=yellow>デバッグモード: Stage {_currentStageID} (Post={_currentIsPost}) を表示します</color>");
        }

        DebugCostom.Log($"<color=cyan>現在のシナリオ: Stage {_currentStageID} {(_currentIsPost ? "戦闘後" : "戦闘前")}</color>");

        if (_currentStageID == 0 || (_currentStageID == 1 && _currentIsPost))
        {
            NavigateToNextScene();
            return;
        }

        LoadAndShowScenario(_currentStageID, _currentIsPost);
        _nextButton.onClick.AddListener(OnNextButtonClicked);
    }

    /// <summary>
    /// Resourcesフォルダからテキストファイルを読み込み、UIに表示します
    /// </summary>
    private void LoadAndShowScenario(int stageID, bool isPost)
    {
        string suffix = isPost ? "End" : "Start";
        string fileName = $"Scenario_{stageID}_{suffix}";

        string filePath = $"Scenarios/{fileName}";
        TextAsset textFile = Resources.Load<TextAsset>(filePath);

        if (textFile != null)
        {
            _mainText.text = textFile.text;
        }
        else
        {
            string errorMsg = $"シナリオファイルが見つかりません: {fileName}";

            DebugCostom.LogWarning(errorMsg);
            _mainText.text = errorMsg;
        }
    }

    /// <summary>
    /// 次へボタンがクリックされた時の処理
    /// </summary>
    public void OnNextButtonClicked()
    {
        NavigateToNextScene();
    }

    /// <summary>
    /// 現在の状態（戦闘前/後）に応じて、適切な次のシーンへ遷移します
    /// </summary>
    private void NavigateToNextScene()
    {
        if (!_currentIsPost)
        {
            // 「戦闘前」だったので、次はデッキ編成（バトル準備）へ
            SceneManager.LoadScene("DeckViewScene");
        }
        else
        {
            // 「戦闘後」だったので、次はホーム（ステージ選択）へ
            SceneManager.LoadScene("HomeScene");
        }
    }
}