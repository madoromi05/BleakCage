using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Battleの流れを管理するクラス 
/// </summary>
public class BattleManager : MonoBehaviour
{
    [SerializeField] private PlayerTurn playerTurn;         // プレイヤーのターンを管理するコンポーネント
    private float turnTime = 10f;                           // プレイヤーのターン時間（秒）
    private EnemyModel enemyModel;                          // 敵のモデル
    private PlayerModel playerModel;                        // プレイヤーのモデル
    private WeaponModel weaponModel;                        // 武器のモデル

    void Start()
    {
        // @Demoサーバーからデータ取得してIDを得たと仮定
        int receivedPlayerId = 1;
        int receivedEnemyId  = 1;
        int receivedWeaponId = 1;

        // Factoryを使ってModelを生成（Entityは内部で読み込み）
        PlayerModelFactory playerFactory = new PlayerModelFactory();
        EnemyModelFactory enemyFactory = new EnemyModelFactory();

        playerModel = playerFactory.CreateFromId(receivedPlayerId);
        enemyModel = enemyFactory.CreateFromId(receivedEnemyId);
        weaponModel = new WeaponModelFactory().CreateFromId(receivedWeaponId);

        if (playerModel == null || enemyModel == null)
        {
            Debug.LogError("プレイヤーまたは敵の初期化に失敗しました");
            return;
        }

        playerTurn.Setup(playerModel, enemyModel ,weaponModel);
        playerTurn.TurnFinished += OnPlayerTurnFinished;
        StartCoroutine(StartPlayerTurnWithTimer());
    }

    /// <summary>
    /// プレイヤーのターンを10秒間開始
    /// </summary>
    private IEnumerator StartPlayerTurnWithTimer()
    {
        Debug.Log("【プレイヤーターン開始】");
        playerTurn.StartPlayerTurn();

        float timer = 0f;
        while (timer < turnTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        playerTurn.FinishPlayerTurn(); // 強制終了（手動でも終了可能）
    }

    /// <summary>
    /// プレイヤーターン終了 → 敵ターン → プレイヤーターン再開
    /// </summary>
    private void OnPlayerTurnFinished()
    {
        Debug.Log("【プレイヤーターン終了】");
        StartCoroutine(EnemyTurn());
    }

    /// <summary>
    /// 敵のターン（仮実装）→ すぐにプレイヤーターン再開
    /// </summary>
    private IEnumerator EnemyTurn()
    {
        Debug.Log("【敵ターン開始】");

        // ここに敵の行動処理を書く（今は0.5秒待機のダミー）
        yield return new WaitForSeconds(1.0f);

        Debug.Log("【敵ターン終了】");

        // 次のプレイヤーターン開始
        StartCoroutine(StartPlayerTurnWithTimer());
    }
}
