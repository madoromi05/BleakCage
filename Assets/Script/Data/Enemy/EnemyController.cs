using UnityEngine;

/// <summary>
/// UI、データをゲームにsetするクラス
/// </summary>
public class EnemyController : MonoBehaviour
{
    private EnemyModel model;
    private EnemyStatusUIController statusUI;
    private Animator animator;
    private AnimatorOverrideController overrideController;

    // アニメーション名を定数化
    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
    private static readonly int DamagedTriggerHash = Animator.StringToHash("DamagedTrigger");
    private static readonly int IsDeadParamHash = Animator.StringToHash("IsDead");

    // アニメーションクリップを定数化
    private const string IdleClipName = "Idle";
    private const string DeathClipName = "Death";
    private const string DamagedClipName = "Damaged";
    private const string AttackClipName = "attack will eilll3 arter";

    private void Awake()
    {
        animator = GetComponent<Animator>();

        // Animator Controllerが設定されているか確認
        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError("AnimatorにベースとなるAnimator Controllerが設定されていません！", this.gameObject);
            return;
        }

        // Override Controllerを初期化
        overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
        animator.runtimeAnimatorController = overrideController;
    }

    /// <summary>
    /// 敵のデータを初期化し、表示とアニメーションを設定する
    /// </summary>
    public void Init(EnemyModel enemyModel)
    {
        this.model = enemyModel;
        // view.Show(model); // [削除]

        // --- [ここから追加 PlayerControllerと同じロジック] ---
        if (model.CharacterPrefab != null)
        {
            // 1. モデルに設定された初期回転（InitialRotation）を取得
            Quaternion desiredLocalRotation = Quaternion.Euler(model.InitialRotation);

            // 2. このEnemyControllerの（土台の）ワールド回転と、子のローカル回転を掛け合わせる
            Quaternion desiredWorldRotation = this.transform.rotation * desiredLocalRotation;

            // 3. 計算したワールド回転を使ってビジュアルモデル(CharacterPrefab)を子として生成
            Instantiate(model.CharacterPrefab, this.transform.position, desiredWorldRotation, this.transform);
        }
        else
        {
            Debug.LogError($"EnemyModel.CharacterPrefabが設定されていません！ (EnemyID: {model.EnemyID})", this.gameObject);
        }
        // --- [ここまで追加] ---


        if (model.EnemyAvatar != null)
        {
            animator.avatar = model.EnemyAvatar;
        }
        else
        {
            // Avatarが設定されていない場合、警告を出す
            if (model.EnemyAnimator != null)
            {
                Debug.LogWarning($"AnimatorSet「{model.EnemyAnimator.name}」にAvatarが設定されていません。", this.gameObject);
            }
            else
            {
                Debug.LogError("EnemyModel.EnemyAnimatorが設定されていません！", this.gameObject);
            }
        }

        // 定数を使ってアニメーションクリップを上書き設定
        overrideController[IdleClipName] = model.EnemyAnimator.Idle;
        overrideController[DeathClipName] = model.EnemyAnimator.Death;
        overrideController[DamagedClipName] = model.EnemyAnimator.Damaged;
    }

    /// <summary>
    /// カードに応じた攻撃アニメーションを再生する
    /// </summary>
    public void PlayAttackAnimation(AnimationClip cardAttackClip)
    {
        if (cardAttackClip != null)
        {
            // 1. 攻撃アニメーションを、カード固有のものに上書き
            overrideController[AttackClipName] = cardAttackClip;

            // 2. 攻撃トリガーを引いて、アニメーションを再生
            animator.SetTrigger(AttackTriggerHash);
        }
        else
        {
            Debug.LogWarning("再生する攻撃アニメーションクリップがありません。", this.gameObject);
        }
    }

    /// <summary>
    /// ダメージを受けた際のアニメーションを再生する
    /// </summary>
    public void PlayDamagedAnimation()
    {
        animator.SetTrigger(DamagedTriggerHash);
    }

    /// <summary>
    /// 死亡時のアニメーションを再生する
    /// </summary>
    public void PlayDeathAnimation()
    {
        animator.SetBool(IsDeadParamHash, true);
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
}