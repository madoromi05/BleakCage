using UnityEngine;
using System.Collections;

public class AnimatorDeathDebugger : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string deadTag = "Dead";   // Tag運用なら
    [SerializeField] private string deadStateName = "Dead"; // Name運用なら
    [SerializeField] private float watchSeconds = 6f;

    private int lastHash;
    private float lastNorm;
    private float lastTime;
    private float lastUnscaledTime;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    public void StartWatch()
    {
        if (animator == null) return;
        StopAllCoroutines();
        StartCoroutine(WatchCoroutine());
    }

    private IEnumerator WatchCoroutine()
    {
        lastHash = 0;
        lastNorm = -1f;
        lastTime = Time.time;
        lastUnscaledTime = Time.unscaledTime;

        float end = Time.unscaledTime + watchSeconds;

        while (Time.unscaledTime < end && animator != null)
        {
            var st = animator.GetCurrentAnimatorStateInfo(0);

            bool isDead = st.IsTag(deadTag) || st.IsName(deadStateName);

            bool stateChanged = st.shortNameHash != lastHash;
            bool normNotMoving = Mathf.Abs(st.normalizedTime - lastNorm) < 0.0001f;

            // 「時間は進んでるのに normalizedTime が進まない」= Animatorが止まってる/Speed0/Culling等
            float dt = Time.time - lastTime;
            float udt = Time.unscaledTime - lastUnscaledTime;

            // 重要イベントだけ出す
            if (stateChanged)
            {
                DebugCostom.Log(
                    $"[AnimDbg] STATE CHANGED hash={st.shortNameHash} norm={st.normalizedTime:F3} len={st.length:F3} " +
                    $"isDead={isDead} timeScale={Time.timeScale} animSpeed={animator.speed} " +
                    $"updateMode={animator.updateMode} culling={animator.cullingMode}",
                    animator
                );
                lastHash = st.shortNameHash;
            }

            // 「止まった疑い」を検知（0.4秒くらい動かなかったら）
            if (udt > 0.4f && normNotMoving)
            {
                DebugCostom.LogWarning(
                    $"[AnimDbg] NORM NOT MOVING norm={st.normalizedTime:F3} (POSSIBLE STOP) " +
                    $"isDead={isDead} timeScale={Time.timeScale} animSpeed={animator.speed} " +
                    $"updateMode={animator.updateMode} culling={animator.cullingMode}",
                    animator
                );
                // 連打防止で一度だけ
                yield break;
            }

            lastNorm = st.normalizedTime;
            lastTime = Time.time;
            lastUnscaledTime = Time.unscaledTime;

            yield return null;
        }
    }
}
