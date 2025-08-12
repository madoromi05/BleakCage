using System.Collections;
using System.Drawing;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
/// <summary>
/// Battleの流れを管理するクラス 
/// </summary>
public class BattleManager : MonoBehaviour
{
    [SerializeField] private PlayerTurn playerTurn;         // プレイヤーのターンを管理するコンポーネント
    [SerializeField] private BattleCardDeck battleDeck;
    [SerializeField] private PlayerCardDeck playerDeck;
    private EnemyModel enemyModel;                          // 敵のモデル
    private PlayerModel playerModel;                        // プレイヤーのモデル
    private PlayerRuntime playerRuntime;
    private WeaponModel weaponModel;                        // 武器のモデル
    private WeaponRuntime weaponRuntime;
    private float turnTime = 10f;                           // プレイヤーのターン時間（秒）

    public TextMeshProUGUI timeText;                        //時間を表示する変数

    void Start()
    {
        // @Demoサーバーからデータ取得してIDを得たと仮定
        int mockPlayerId = 1;
        int mockEnemyId  = 1;
        int mockWeaponId = 1;

        // Factoryを使ってModelを生成（Entityは内部で読み込み）
        PlayerModelFactory playerFactory = new PlayerModelFactory();
        EnemyModelFactory enemyFactory = new EnemyModelFactory();

        playerModel = playerFactory.CreateFromId(mockPlayerId);
        enemyModel = enemyFactory.CreateFromId(mockEnemyId);
        weaponModel = new WeaponModelFactory().CreateFromId(mockWeaponId);

        IAttackStrategy defaultStrategy = new AttributeWeakness();
        playerRuntime = new PlayerRuntime(playerModel, defaultStrategy);
        weaponRuntime = new WeaponRuntime(weaponModel);

        // プレイヤーに武器を装備させる
        playerRuntime.EquipWeapon(weaponRuntime);

        // PlayerDeck
        playerTurn.Setup(playerRuntime, weaponRuntime, enemyModel, playerDeck, battleDeck);
        playerTurn.TurnFinished += OnPlayerTurnFinished;
        StartPlayerTurn();
    }

    /// <summary>
    /// Playerターン開始時の処理
    /// </summary>
    private void StartPlayerTurn()
    {
        turnTime = 10f;
        StartCoroutine(StartPlayerTurnWithTimer());
    }

    /// <summary>
    /// プレイヤーのターンを10秒間開始
    /// </summary>
    private IEnumerator StartPlayerTurnWithTimer()
    {
         Debug.Log("【プレイヤーターン開始】");
        playerTurn.StartPlayerTurn();

        while (turnTime >= 0)
        {
            turnTime -= Time.deltaTime;
            timeText.text = turnTime.ToString("f2") + " <size=70%>SECOND</size>";
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
        StartPlayerTurn();
    }
}
