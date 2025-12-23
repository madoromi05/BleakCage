using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BattleCardDebugger : MonoBehaviour
{
    [Header("参照設定 (インスペクターで登録してください)")]
    [SerializeField] private DamageCalculator damageCalculator;

    // ▼ 変更: 直接コントローラーを登録するリストに変更
    [SerializeField] private List<PlayerController> playerControllers;
    [SerializeField] private List<EnemyController> enemyControllers;

    // UI更新のために必要なら残す（不要なら削除可）
    [SerializeField] private List<PlayerStatusUIController> playerUIs;
    [SerializeField] private List<EnemyStatusUIController> enemyUIs;

    [Header("テスト設定")]
    public int TestCardID = 607;
    public int AttackerPlayerIndex = 0;
    public int TargetEnemyIndex = 0;

    private CardModelFactory cardModelFactory;

    private void Awake()
    {
        cardModelFactory = new CardModelFactory();
    }

    public void ExecuteTest()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("プレイモード中のみ使用可能です。");
            return;
        }
        StartCoroutine(ExecuteCardRoutine());
    }

    private IEnumerator ExecuteCardRoutine()
    {
        // 1. インデックスのチェック
        if (AttackerPlayerIndex >= playerControllers.Count || TargetEnemyIndex >= enemyControllers.Count)
        {
            Debug.LogError("指定したインデックスのキャラ/敵がリストに登録されていません。Inspectorを確認してください。");
            yield break;
        }

        // 2. ControllerからRuntimeとTransformを取得
        PlayerController attackerCtrl = playerControllers[AttackerPlayerIndex];
        EnemyController targetCtrl = enemyControllers[TargetEnemyIndex];

        PlayerRuntime playerRuntime = null;
        if (playerUIs.Count > AttackerPlayerIndex && playerUIs[AttackerPlayerIndex] != null)
        {
            playerRuntime = playerUIs[AttackerPlayerIndex].GetPlayerRuntime();
        }

        // とりあえず既存コードに合わせてUIから取る場合のバックアップ
        if (playerRuntime == null && playerUIs.Count > AttackerPlayerIndex)
        {
            playerRuntime = playerUIs[AttackerPlayerIndex].GetPlayerRuntime();
        }

        var enemyRuntime = targetCtrl.GetComponent<EnemyRuntime>() ?? targetCtrl.GetComponentInParent<EnemyRuntime>();
        Transform targetTransform = targetCtrl.transform;

        if (playerRuntime == null) { Debug.LogError("PlayerRuntimeが見つかりません"); yield break; }
        if (enemyRuntime == null) { Debug.LogError("EnemyRuntimeが見つかりません"); yield break; }

        // 3. カードモデル生成
        CardModel model = cardModelFactory.CreateFromID(TestCardID);
        if (model == null) { Debug.LogError($"カードID {TestCardID} が見つかりません"); yield break; }

        Debug.Log($"<color=cyan>--- テスト開始: {model.Name} ---</color>");

        // 4. ダミーデータ生成
        // ダミー武器
        WeaponRuntime dummyWeapon = new WeaponRuntime(new WeaponModel
        {
            AttackPower = 10,
            Attribute = model.Attribute,
            WeaponPrefab = null
        }, System.Guid.NewGuid().ToString());

        // ★修正: setアクセサエラー回避のため、WeaponRuntime側で set を許可している必要があります
        dummyWeapon.ParentPlayer = playerRuntime;

        // ダミーカード
        CardRuntime dummyCard = new CardRuntime(model, System.Guid.NewGuid().ToString());
        dummyCard.weaponRuntime = dummyWeapon;

        // 5. コマンド実行
        ICommand command = null;

        if (IsAttackAttribute(model.Attribute))
        {
            command = new AttackCommand(
                playerRuntime,
                dummyWeapon,
                dummyCard,
                enemyUIs.Count > TargetEnemyIndex ? enemyUIs[TargetEnemyIndex] : null,
                playerUIs.Count > AttackerPlayerIndex ? playerUIs[AttackerPlayerIndex] : null,
                enemyRuntime,
                targetTransform,
                damageCalculator,
                cardModelFactory
            );
        }
        else
        {
            // 支援・バフ
            PlayerStatusUIController ui = playerUIs.Count > AttackerPlayerIndex ? playerUIs[AttackerPlayerIndex] : null;

            switch (model.Attribute)
            {
                case AttributeType.Heal:
                    command = new HealCommand(playerRuntime, dummyCard, ui, model);
                    break;
                default: // Buff系
                    command = new BuffCommand(playerRuntime, dummyCard, ui, model);
                    break;
            }
        }

        if (command != null)
        {
            yield return command.Do();
            Debug.Log("--- テスト終了 ---");
        }
    }

    private bool IsAttackAttribute(AttributeType attribute)
    {
        switch (attribute)
        {
            case AttributeType.Slash:
            case AttributeType.Blunt:
            case AttributeType.Pierce:
            case AttributeType.Bullet:
                return true;
            default:
                return false;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(BattleCardDebugger))]
public class BattleCardDebuggerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        BattleCardDebugger d = (BattleCardDebugger)target;
        GUILayout.Space(10);
        if (GUILayout.Button("Test Card")) d.ExecuteTest();
    }
}
#endif