using UnityEngine;
using DG.Tweening;
using System.Collections;

/// <summary>
/// UI、データをゲームにsetするクラス
/// </summary>
public class EnemyController : MonoBehaviour
{
    public event System.Action OnAttackHitMoment;
    private EnemyModel model;
    private EnemyStatusUIController statusUI;
    private Animator animator;
    private AnimatorOverrideController overrideController;
    private Vector3 originalPosition; // 攻撃前の元の位置
    private Transform playerTransform; // 攻撃対象（プレイヤー）のTransform

    // AnimatorのParametersタブと一致させる
    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
    private static readonly int AttackIDHash = Animator.StringToHash("AttackID");

    // Animatorのステート名と一致させる
    private const string IdleClipName = "Idle";
    private const string AttackClipName001 = "Attack001";
    private const string AttackClipName002 = "Attack002";
    private const string AttackClipName003 = "Attack003";

    /// <summary>
    /// 敵のデータを初期化し、表示とアニメーションを設定する
    /// </summary>
    public void Init(EnemyModel enemyModel, Transform targetPlayer)
    {
        this.model = enemyModel;
        this.playerTransform = targetPlayer;
        this.originalPosition = transform.position;

        if (model.CharacterPrefab != null)
        {
            Quaternion desiredLocalRotation = Quaternion.Euler(model.InitialRotation);
            Quaternion desiredWorldRotation = this.transform.rotation * desiredLocalRotation;
            // プレハブを生成し、その参照(instance)を保持する
            GameObject instance = Instantiate(model.CharacterPrefab, this.transform.position, desiredWorldRotation, this.transform);

            // 生成したインスタンスから "Animator" を取得する
            this.animator = instance.GetComponent<Animator>();
            if (this.animator == null)
            {
                Debug.LogError("生成したキャラクタープレハブに Animator がありません！", instance);
                return;
            }
        }
        else
        {
            Debug.LogError($"EnemyModel.CharacterPrefabが設定されていません！ (EnemyID: {model.EnemyID})", this.gameObject);
            return; // Animatorがないのでここで処理終了
        }

        // "animator" が取得できたので、OverrideController の設定を "Init" で行う
        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError("キャラクターのAnimatorにベースとなるAnimator Controllerが設定されていません！", this.animator.gameObject);
            return;
        }
        overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
        animator.runtimeAnimatorController = overrideController;

        // クリップの上書き (model.EnemyAnimator は "EnemyAnimatorSet" 型)
        if (model.EnemyAnimator != null)
        {
            overrideController[IdleClipName] = model.EnemyAnimator.Idle;
            overrideController[AttackClipName001] = model.EnemyAnimator.Attack001;
            overrideController[AttackClipName002] = model.EnemyAnimator.Attack002;
            overrideController[AttackClipName003] = model.EnemyAnimator.Attack003;
        }
        else
        {
            Debug.LogError($"EnemyModel (ID: {model.EnemyID}) に EnemyAnimator が設定されていません。");
        }
    }

    /// <summary>
    /// 攻撃シーケンスを開始する
    /// </summary>
    public IEnumerator AttackSequence()
    {
        // 1. 相手の懐（例: 1.5ユニット手前）まで移動する
        Vector3 targetPosition = playerTransform.position + (transform.position - playerTransform.position).normalized * 1.5f;
        float moveDuration = 0.3f; // 0.3秒かけて移動

        transform.DOMove(targetPosition, moveDuration).SetEase(Ease.OutCubic);

        // 移動が終わるまで待機
        yield return new WaitForSeconds(moveDuration);

        // 2. 攻撃アニメーションを再生
        animator.SetTrigger("Attack"); // 仮のアニメーショントリガー

        // 3. アニメーション再生が完了するまで待機（ここは後述の「アニメーションイベント」で制御するのがベスト）
        // とりあえず固定時間待機する場合
        float attackAnimLength = 1.0f; // アニメの長さに合わせる
        yield return new WaitForSeconds(attackAnimLength);

        // 4. 元の位置に戻る
        float returnDuration = 0.5f;
        transform.DOMove(originalPosition, returnDuration).SetEase(Ease.InOutQuad);

        // 戻るまで待機
        yield return new WaitForSeconds(returnDuration);

        // これで攻撃シーケンス完了
        Debug.Log("攻撃完了、元の位置に戻りました");
    }

    /// <summary>
    /// OverrideControllerに設定されたクリップの長さを取得する
    /// </summary>
    private float GetAnimationClipLength(string clipNameKey)
    {
        if (overrideController != null && overrideController[clipNameKey] != null)
        {
            return overrideController[clipNameKey].length;
        }
        Debug.LogWarning($"EnemyのOverrideControllerに '{clipNameKey}' のクリップが見つかりません。デフォルトの0.5秒を使用します。", this);
        return 0.5f;
    }

    /// <summary>
    /// ランダムな攻撃アニメーションを再生し、その長さを返す
    /// (EnemyAttackCommand から呼ばれる)
    /// </summary>
    /// <returns>再生したアニメーションの長さ(秒)</returns>
    public float PlayRandomAttackAnimation()
    {
        // 0, 1, 2 のいずれかをランダムに生成
        int randomAttackID = Random.Range(0, 3);

        // --- [デバッグログ] ---
        Debug.Log($"[EnemyController] アニメーション再生: AttackID = {randomAttackID}");
        if (animator == null)
        {
            Debug.LogError("[EnemyController] Animatorがnullです！ Init()の処理が正しく完了していません。");
            return 0.5f;
        }
        // ---

        // Animatorにパラメータを設定
        animator.SetInteger(AttackIDHash, randomAttackID);
        animator.SetTrigger(AttackTriggerHash);

        // 再生したアニメーションの長さを返す
        switch (randomAttackID)
        {
            case 0:
                return GetAnimationClipLength(AttackClipName001);
            case 1:
                return GetAnimationClipLength(AttackClipName002);
            case 2:
                return GetAnimationClipLength(AttackClipName003);
            default:
                return 0.5f; // 安全なデフォルト値
        }
    }

    public void SetStatusUI(EnemyStatusUIController ui)
    {
        this.statusUI = ui;
    }

    public void UpdateHealthUI(float currentHP)
    {
        if (statusUI != null)
        {
            statusUI.UpdateHP(currentHP);
        }
    }

    /// <summary>
    /// アニメーションイベントから呼び出される関数
    /// </summary>
    public void TriggerAttackHit()
    {
        // 攻撃が当たる瞬間にイベントを発火させる
        OnAttackHitMoment?.Invoke();
    }
}