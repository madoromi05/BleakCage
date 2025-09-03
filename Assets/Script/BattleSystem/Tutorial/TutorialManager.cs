using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Tutorialの流れを管理するクラス
/// </summary>
public class TutorialManager : MonoBehaviour
{
    [SerializeField] private GameObject tutorialUIPanel;
    [SerializeField] private TextMeshProUGUI tutorialText;

    private BattleInputReader inputReader;

    private BattleManager battleManager;
    private PlayerTurn playerTurn;
    private EnemyTurn enemyTurn;

    private bool hasSelectedCard = false; // プレイヤーがカードを選択したかどうかのフラグ
    private bool isPlayerTurnFinished = false;

    /// <summary>
    /// BattleManagerから呼ばれてチュートリアルを開始する
    /// </summary>
    public void StartTutorialFlow(BattleManager bm, PlayerTurn pt, EnemyTurn et)
    {
        this.battleManager = bm;
        this.playerTurn = pt;
        this.enemyTurn = et;
        StartCoroutine(TutorialCoroutine());
    }

    private IEnumerator TutorialCoroutine()
    {
        tutorialUIPanel.SetActive(true);

        // --- 1. カード選択を促す ---
        battleManager.StartPlayerTurnForTutorial(); // タイマーなしでプレイヤーのターン開始
        playerTurn.OnCardSelected += HandleCardSelection; // カード選択イベントを購読
        hasSelectedCard = false;

        tutorialText.text = "このフェーズでは、どのキャラが、どの敵を攻撃するかを決定します。";
        tutorialText.text = "敵全体の情報はここで確認できます。また、詳細情報は、閲覧したい敵にカーソルを合わせるとここに表示されます。";
        tutorialText.text = "同様に、味方全体の情報はこちらで確認できます。詳細情報は、閲覧したい味方キャラクターにカーソルを合わせることで表示されます。";

        // プレイヤーがカードを選択するまで待機
        yield return new WaitUntil(() => hasSelectedCard);

        playerTurn.OnCardSelected -= HandleCardSelection; // 購読解除

        tutorialText.text = "素晴らしい！\nカードを選んだら「Space」キーで決定です。";
        // ここでは決定まで待たずに次に進んでも良いし、待つことも可能

        playerTurn.OnTurnFinished += OnPlayerTurnFinished;
        isPlayerTurnFinished = false;
        yield return new WaitUntil(() => isPlayerTurnFinished);
        playerTurn.OnTurnFinished -= OnPlayerTurnFinished;

        Debug.Log("プレイヤーのターン終了。");

        // --- 2. 攻撃優先順位の説明（将来的な機能の場所） ---
        tutorialText.text = "（将来的にはここで、攻撃するキャラクターの優先順位を決めます）";
        yield return new WaitForSeconds(3.0f);


        // --- 3. 敵ターン開始時 ---
        tutorialText.text = "次は敵のターンです。";
        yield return new WaitForSeconds(2.0f);


        // --- 4. 敵ターン途中 ---
        enemyTurn.StartEnemyTurn(); // 敵の行動を開始
        tutorialText.text = "敵が攻撃してきます！防御の準備を！";
        yield return new WaitForSeconds(2.0f); // 敵の攻撃アニメーションなどを待つ時間


        // --- 5. 敵ターン終了時 ---
        tutorialText.text = "敵のターンが終了しました。\nこれでチュートリアルは終わりです。";
        yield return new WaitForSeconds(3.0f);

        tutorialUIPanel.SetActive(false);
        Debug.Log("チュートリアル完了");
    }

    /// <summary>
    /// PlayerTurn.TurnFinishedイベントが発火したときに呼び出されるメソッド
    /// </summary>
    private void OnPlayerTurnFinished()
    {
        // フラグをtrueにする
        isPlayerTurnFinished = true;
    }

    // PlayerTurnのOnCardSelectedイベントによって呼び出される
    private void HandleCardSelection(int cardIndex)
    {
        hasSelectedCard = true;
    }
}