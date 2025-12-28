using System.Collections;
using UnityEngine;
using DG.Tweening;

[DisallowMultipleComponent]
public class PlayerDeathController : MonoBehaviour
{
    [Header("演出")]
    [SerializeField] private bool vanishByMove = true; // falseなら最後の姿で固定
    [SerializeField] private float deadMoveDuration = 1.0f;
    [SerializeField] private Vector3 deadTargetPosition = new Vector3(-30f, 0f, 10f);

    [Header("Animator")]
    [SerializeField] private string deadTrigger = "IsDead";
    [SerializeField] private bool disableAnimatorAfterDead = true;

    private bool isDead = false;
    private Animator anim;

    private PlayerMovementController moveCtrl;
    private PlayerCombatController combatCtrl;
    private PlayerController playerCtrl;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        moveCtrl = GetComponent<PlayerMovementController>();
        combatCtrl = GetComponent<PlayerCombatController>();
        playerCtrl = GetComponent<PlayerController>();
    }

    public IEnumerator DeadSequence()
    {
        if (isDead) yield break;
        isDead = true;

        // ① 割り込み元を止める（Deadが途中で切れる最大原因を潰す）
        if (moveCtrl != null) moveCtrl.enabled = false;
        if (combatCtrl != null) combatCtrl.enabled = false;
        if (playerCtrl != null) playerCtrl.enabled = false;

        // ② 衝突を止める
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // ③ 死亡アニメ開始
        if (anim != null)
        {
            anim.SetTrigger(deadTrigger);
        }

        // 1フレーム待って遷移を確定
        yield return null;

        // 最後の姿で固定 or 退場
        if (!vanishByMove)
        {
            // アニメを最後まで見せたいなら適当に待機（timeScaleの影響を受けない）
            // ここは必要なら「Deadステート長を取って待つ」にしても良い
            yield return new WaitForSecondsRealtime(1.0f);

            if (disableAnimatorAfterDead && anim != null)
                anim.enabled = false;

            yield break;
        }

        // Enemyと同じ：移動して消す
        Sequence seq = DOTween.Sequence();
        seq.Join(transform.DOMove(deadTargetPosition, deadMoveDuration).SetEase(Ease.InBack));
        var renderers = GetComponentsInChildren<Renderer>();
        yield return seq.WaitForCompletion();
        gameObject.SetActive(false);
    }
}
