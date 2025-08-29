using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
/// <summary>
/// Battleの流れを管理するクラス 
/// </summary>
public class TuterealBattleManager : MonoBehaviour
{
    public TextMeshProUGUI timeText;                        //時間を表示する変数

    [SerializeField] private PlayerTurn playerTurn;
    [SerializeField] private BattleCardDeck battleDeck;
    [SerializeField] private PlayerProfile playerProfile; // プレイヤーデッキ

    private List<PlayerRuntime> party = new List<PlayerRuntime>();
    private EnemyModel enemyModel;
    private EnemyModelFactory enemyFactory;
    private PlayerModel playerModel;
    private PlayerRuntime playerRuntime;
    private PlayerModelFactory playerFactory;
    private WeaponModel weaponModel;
    private WeaponRuntime weaponRuntime;
    private WeaponModelFactory weaponFactory;
    private CardModelFactory cardFactory;
    private float turnTime = 10f;                           // プレイヤーのターン時間（秒）

    void Start()
    {
        playerFactory = new PlayerModelFactory();
        enemyFactory = new EnemyModelFactory();
        weaponFactory = new WeaponModelFactory();
        cardFactory = new CardModelFactory();

        // 3. 読み込んだデータからランタイムの階層構造を構築
        IAttackStrategy defaultStrategy = new AttributeWeakness();
        List<CardRuntime> allCardsForBattle = new List<CardRuntime>();

        foreach (var charData in playerProfile.BattleCharacters)
        {
            PlayerModel playerModel = playerFactory.CreateFromId(charData.CharacterId);

            if (playerModel == null)
            {
                Debug.LogError($"PlayerModelの生成に失敗しました。ID: {charData.CharacterId} のPlayerEntityがResources内に存在するか確認してください。");
                continue; // このキャラクターの処理をスキップして次に進む
            }

            PlayerRuntime playerRuntime = new PlayerRuntime(playerModel, defaultStrategy, charData.InstanceId);
            party.Add(playerRuntime);

            // プレイヤーが直接持つカードを「見えない武器」にセット
            if (charData.EquippedCards != null)
            {
                foreach (var cardData in charData.EquippedCards)
                {
                    CardModel cardModel = cardFactory.CreateFromID(cardData.CardId);
                    CardRuntime cardRuntime = new CardRuntime(cardModel, cardData.InstanceId);
                    playerRuntime.InnateWeapon.AddCard(cardRuntime); // InnateWeaponにカードを追加
                    allCardsForBattle.Add(cardRuntime);
                }
            }

            foreach (var weaponData in charData.EquippedWeapons)
            {
                WeaponModel weaponModel = weaponFactory.CreateFromId(weaponData.WeaponId);
                WeaponRuntime weaponRuntime = new WeaponRuntime(weaponModel, weaponData.InstanceId);
                playerRuntime.EquipWeapon(weaponRuntime); // Player -> Weapon の親子関係を確立

                foreach (var cardData in weaponData.SlottedCards)
                {
                    CardModel cardModel = cardFactory.CreateFromID(cardData.CardId);
                    CardRuntime cardRuntime = new CardRuntime(cardModel, cardData.InstanceId);
                    weaponRuntime.AddCard(cardRuntime); // Weapon -> Card の親子関係を確立
                    allCardsForBattle.Add(cardRuntime);
                }
            }
        }

        int mockEnemyId = 1;
        enemyModel = enemyFactory.CreateFromId(mockEnemyId);

        battleDeck.InitFromCardList(allCardsForBattle);

        // PlayerDeck生成
        playerTurn.Setup(party[0], enemyModel, battleDeck);
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
