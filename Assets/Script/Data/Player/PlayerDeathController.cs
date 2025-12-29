using System.Collections;
using UnityEngine;
using DG.Tweening;

public class PlayerDeathController : MonoBehaviour
{
    [Header("演出")]
    [SerializeField] private bool vanishByMove = false;
    [SerializeField] private float deadMoveDuration = 1.0f;
    [SerializeField] private Vector3 deadTargetPosition = new Vector3(-30f, 0f, 10f);

    [Header("Animator")]
    [SerializeField] private bool disableAnimatorAfterDead = true;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLog = true;

    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

    private bool isDead = false;

    // ★ Awake では取れないことがあるので、後から差し込めるようにする
    private Animator anim;

    private PlayerMovementController moveCtrl;
    private PlayerCombatController combatCtrl;
    private PlayerController playerCtrl;

    private void Awake()
    {
        moveCtrl = GetComponent<PlayerMovementController>();
        combatCtrl = GetComponent<PlayerCombatController>();
        playerCtrl = GetComponent<PlayerController>();

        if (enableDebugLog)
            Debug.Log($"[PDC] Awake obj={name} (anim will be assigned later)", this);
    }

    // ★ PlayerController.Init() から呼ぶ
    public void SetAnimator(Animator animator)
    {
        anim = animator;
        if (anim != null)
        {
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (enableDebugLog)
                Debug.Log($"[PDC] Animator assigned: {anim.gameObject.name}", this);
        }
        else
        {
            Debug.LogWarning("[PDC] SetAnimator called with null", this);
        }
    }

    public IEnumerator DeadSequence()
    {
        if (isDead) yield break;
        isDead = true;

        if (enableDebugLog) Debug.Log("[PDC] DeadSequence START", this);

        if (moveCtrl) moveCtrl.enabled = false;
        if (combatCtrl) combatCtrl.enabled = false;
        if (playerCtrl) playerCtrl.enabled = false;

        var col = GetComponent<Collider>();
        if (col) col.enabled = false;

        // ★ ここで最終取得（SetAnimator漏れ対策）
        if (anim == null)
        {
            anim = GetComponentInChildren<Animator>(true);
        }

        if (anim == null)
        {
            Debug.LogError("[PDC] Animator not found (character prefabにAnimatorが居るか/生成されているか確認)", this);
            yield break;
        }

        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        // Trigger捨ててBoolのみ
        anim.SetBool(IsDeadHash, true);

        if (enableDebugLog)
            StartCoroutine(LogAnimatorWhileDying(3f));

        yield return null;

        // その場に残る
        if (!vanishByMove)
        {
            // Deadに入るまで待つ
            float enterTimeout = 1.0f;
            while (enterTimeout > 0f)
            {
                var st0 = anim.GetCurrentAnimatorStateInfo(0);
                if (st0.IsTag("Dead") || st0.IsName("Dead")) break;
                enterTimeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            // 最後まで待つ
            while (true)
            {
                if (anim == null) yield break;
                if (anim.IsInTransition(0)) { yield return null; continue; }

                var st = anim.GetCurrentAnimatorStateInfo(0);
                bool isDeadState = st.IsTag("Dead") || st.IsName("Dead");
                if (!isDeadState) break;

                if (st.normalizedTime >= 1.0f) break;
                yield return null;
            }

            if (disableAnimatorAfterDead)
            {
                anim.enabled = false;
            }
            yield break;
        }

        // 退場
        Sequence seq = DOTween.Sequence();
        seq.Join(transform.DOMove(deadTargetPosition, deadMoveDuration).SetEase(Ease.InBack));
        seq.SetLink(gameObject);
        yield return seq.WaitForCompletion();
        gameObject.SetActive(false);
    }

    private IEnumerator LogAnimatorWhileDying(float seconds)
    {
        float end = Time.unscaledTime + seconds;
        while (Time.unscaledTime < end && anim != null)
        {
            var st = anim.GetCurrentAnimatorStateInfo(0);
            Debug.Log(
                $"[PDC-LOG] hash={st.shortNameHash} norm={st.normalizedTime:F3} inTrans={anim.IsInTransition(0)} " +
                $"deadTag={st.IsTag("Dead")} enabled={anim.enabled} speed={anim.speed}",
                this
            );
            yield return new WaitForSecondsRealtime(0.2f);
        }
    }
}
