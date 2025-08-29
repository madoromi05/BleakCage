using UnityEngine;
using System.Collections;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    // チュートリアルの進行状況を管理するenum
    public enum TutorialStep
    {
        None,
        Start,
        ExplainAttackPriority, // 攻撃優先度の説明
        WaitForAttackPriority, // 優先度設定を待つ
        ExplainCardSelection,  // カード選択の説明
        WaitForSpecificCards,  // 特定カードの選択を待つ
        AllowFreeSelection,    // 自由なカード選択を許可
        ExplainEnemyTurn,      // 敵ターンの説明
        End
    }

    public TutorialStep currentStep = TutorialStep.None;

    // UI要素への参照（Inspectorで設定）
    public GameObject explanationWindow;
    public TextMeshPro explanationText;

    private bool isWaitingForPlayerAction = false;

    // チュートリアルを開始する
    public void StartTutorial()
    {
        Debug.Log("チュートリアル開始！");
        currentStep = TutorialStep.Start;
        StartCoroutine(TutorialFlow());
    }

    // チュートリアルの流れを管理するコルーチン
    private IEnumerator TutorialFlow()
    {
        while (currentStep != TutorialStep.End)
        {
            isWaitingForPlayerAction = false;

            switch (currentStep)
            {
                case TutorialStep.Start:
                    // 最初の説明
                    Time.timeScale = 0f; // 時間を止める
                    ShowExplanation("ここからはチュートリアルが始まります。");
                    // UIのボタンが押されるのを待つ処理などをここに追加
                    AdvanceStep(); // 次のステップへ
                    break;

                case TutorialStep.ExplainAttackPriority:
                    ShowExplanation("このフェーズでは、どのキャラが、どの敵を攻撃するかを決定します。");
                    // UI点滅などの演出を開始
                    isWaitingForPlayerAction = true;
                    break;

                    // ... 他のステップの処理を追加 ...
            }

            // プレイヤーの操作を待つ
            yield return new WaitUntil(() => !isWaitingForPlayerAction);
        }
    }

    // 説明ウィンドウを表示する
    private void ShowExplanation(string text)
    {
        explanationWindow.SetActive(true);
        explanationText.text = text;
        // プレイヤーがウィンドウを閉じたら Time.timeScale = 1f; に戻す処理も必要
    }

    // プレイヤーのアクションが完了したときに外部から呼ばれる
    public void PlayerActionCompleted()
    {
        if (isWaitingForPlayerAction)
        {
            isWaitingForPlayerAction = false;
            AdvanceStep();
        }
    }

    // ステップを進める
    private void AdvanceStep()
    {
        currentStep++; // enumの次の段階へ
    }
}