using System.Collections;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// プレイヤーの死亡演出とステート管理を制御するクラス
/// </summary>
public class PlayerDeathController : MonoBehaviour
{
    [Header("演出設定")]
    [SerializeField] private bool _vanishByMove = false;                                // 移動して退場するかどうか
    [SerializeField] private float _deadMoveDuration = 1.0f;                            // 退場移動にかかる時間
    [SerializeField] private Vector3 _deadTargetPosition = new Vector3(-30f, 0f, 10f);  // 退場目標地点

    [Header("Animator設定")]
    [SerializeField] private bool _disableAnimatorAfterDead = true;                     // 死亡アニメーション終了後にAnimatorを無効化するか

    [Header("デバッグ設定")]
    [SerializeField] private bool _enableDebugLog = true;

    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

    private bool _isDead = false;
    private Animator _anim;

    private PlayerMovementController _moveCtrl;
    private PlayerCombatController _combatCtrl;
    private PlayerController _playerCtrl;

    private void Awake()
    {
        _moveCtrl = GetComponent<PlayerMovementController>();
        _combatCtrl = GetComponent<PlayerCombatController>();
        _playerCtrl = GetComponent<PlayerController>();

        if (_enableDebugLog)
        {
            DebugCostom.Log($"[PDC] Awake obj={name} (anim will be assigned later)", this);
        }
    }

    /// <summary>
    /// 外部（生成時など）からAnimatorをセットします
    /// </summary>
    public void SetAnimator(Animator animator)
    {
        _anim = animator;
        if (_anim != null)
        {
            _anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            if (_enableDebugLog)
                DebugCostom.Log($"[PDC] Animator assigned: {_anim.gameObject.name}", this);
        }
        else
        {
            DebugCostom.LogWarning("[PDC] SetAnimator called with null", this);
        }
    }

    /// <summary>
    /// 死亡シーケンスを実行します
    /// </summary>
    public IEnumerator DeadSequence()
    {
        if (_isDead) yield break;
        _isDead = true;

        if (_enableDebugLog)
        {
            DebugCostom.Log("[PDC] DeadSequence START", this);
        }

        // 全ての操作・戦闘スクリプトを無効化
        if (_moveCtrl) _moveCtrl.enabled = false;
        if (_combatCtrl) _combatCtrl.enabled = false;
        if (_playerCtrl) _playerCtrl.enabled = false;

        // 当たり判定を無効化
        var col = GetComponent<Collider>();
        if (col) col.enabled = false;

        if (_anim == null)
        {
            _anim = GetComponentInChildren<Animator>(true);
        }

        if (_anim == null)
        {
            DebugCostom.LogError("[PDC] Animator not found (character prefabにAnimatorが居るか/生成されているか確認)", this);
            yield break;
        }

        // 死亡アニメーションを確実に再生させる設定
        _anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        _anim.SetBool(IsDeadHash, true);

        if (_enableDebugLog)
            StartCoroutine(LogAnimatorWhileDying(3f));

        yield return null;

        // その場に留まって死亡する処理
        if (!_vanishByMove)
        {
            // "Dead"ステートに遷移し始めるまで待機（タイムアウト付き）
            float enterTimeout = 1.0f;
            while (enterTimeout > 0f)
            {
                var st0 = _anim.GetCurrentAnimatorStateInfo(0);
                if (st0.IsTag("Dead") || st0.IsName("Dead")) break;
                enterTimeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            // 死亡アニメーションが最後まで再生されるのを待つ
            while (true)
            {
                if (_anim == null) yield break;

                if (_anim.IsInTransition(0)) { yield return null; continue; }

                var st = _anim.GetCurrentAnimatorStateInfo(0);
                bool isDeadState = st.IsTag("Dead") || st.IsName("Dead");

                if (!isDeadState) break;
                if (st.normalizedTime >= 1.0f) break;

                yield return null;
            }

            // 最終的にAnimatorを停止させて負荷を減らす
            if (_disableAnimatorAfterDead)
            {
                _anim.enabled = false;
            }
            yield break;
        }

        Sequence seq = DOTween.Sequence();
        seq.Join(transform.DOMove(_deadTargetPosition, _deadMoveDuration).SetEase(Ease.InBack));
        seq.SetLink(gameObject);

        yield return seq.WaitForCompletion();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 死亡中のAnimatorステートを一定時間監視ログに出力します
    /// </summary>
    private IEnumerator LogAnimatorWhileDying(float seconds)
    {
        float end = Time.unscaledTime + seconds;
        while (Time.unscaledTime < end && _anim != null)
        {
            yield return new WaitForSecondsRealtime(0.2f);
        }
    }
}