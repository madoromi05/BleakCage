using UnityEngine;
using System.Collections;
using System;

[RequireComponent(typeof(PlayerAnimationController))]
[RequireComponent(typeof(PlayerMovementController))]
[RequireComponent(typeof(PlayerCombatController))]
public class PlayerController : MonoBehaviour
{
    [Header("Death Debug")]
    [SerializeField] private bool _logDeathDebug = false;
    [SerializeField] private float _deathLogDuration = 3.0f;

    public bool IsDead { get; private set; } = false;

    private PlayerAnimationController _animCtrl;
    private PlayerMovementController _moveCtrl;
    private PlayerCombatController _combatCtrl;
    private PlayerStatusUIController _statusUi;

    private Transform _characterRoot;
    private float _guardVisualYOffset = 0f;
    private bool _isGuardVisualRaised = false;
    private Coroutine _guardVisualCoroutine;
    private float _guardHoldUntilTime = 0f;
    private bool _isGuardHeld = false;
    private Vector3 _guardBaseLocalPos;
    private Animator _animator;

    // ★追加：死亡中は他のアニメ更新を完全停止する
    private bool _isInDeathSequence = false;

    public void SetStatusUI(PlayerStatusUIController ui) => _statusUi = ui;

    // イベント中継
    public event Action OnAttackHitTriggered
    {
        add => _combatCtrl.OnAttackHitTriggered += value;
        remove => _combatCtrl.OnAttackHitTriggered -= value;
    }

    private void Awake()
    {
        _animCtrl = GetComponent<PlayerAnimationController>();
        _moveCtrl = GetComponent<PlayerMovementController>();
        _combatCtrl = GetComponent<PlayerCombatController>();
    }

    public void Init(PlayerModel model)
    {
        _moveCtrl.Init(transform.localPosition);
        _guardVisualYOffset = model != null ? model.GuardVisualYOffset : 0f;

        if (model.CharacterPrefab != null)
        {
            GameObject instance = Instantiate(model.CharacterPrefab, transform);
            _characterRoot = instance.transform;

            instance.transform.localPosition = model.CharacterPrefab.transform.localPosition;
            instance.transform.localScale = model.CharacterPrefab.transform.localScale;

            Quaternion prefabRot = model.CharacterPrefab.transform.localRotation;
            Quaternion adjustRot = Quaternion.Euler(model.InitialRotation);
            instance.transform.localRotation = prefabRot * adjustRot;

            _guardBaseLocalPos = _characterRoot.localPosition;
            _isGuardVisualRaised = false;

            Animator anim = instance.GetComponent<Animator>();
            _animator = anim;

            CharacterBoneHolder boneHolder = instance.GetComponent<CharacterBoneHolder>();
            Transform rightHandSocket = boneHolder != null ? boneHolder.RightHandTransform : null;
            Transform leftHandSocket = boneHolder != null ? boneHolder.LeftHandTransform : null;

            if (anim != null)
            {
                _animCtrl.Init(anim);
            }

            _combatCtrl.Init(_animCtrl, _moveCtrl, rightHandSocket, leftHandSocket);
        }
    }

    public void SetInitialWeapon(WeaponRuntime weaponRuntime)
    {
        if (_combatCtrl != null && weaponRuntime != null)
        {
            _combatCtrl.EquipMainWeapon(weaponRuntime);
        }
    }

    public IEnumerator AttackSequence(CardModel cardModel, WeaponRuntime weaponRuntime, Transform target)
    {
        if (IsDead) yield break;
        yield return _combatCtrl.ExecuteAttackSequence(cardModel, weaponRuntime, target);
    }

    public IEnumerator SupportEffect(CardModel cardModel)
    {
        if (IsDead) yield break;
        yield return _combatCtrl.ExecuteSupportEffect(cardModel);
    }

    public void SetGuardAnimation(bool isGuarding)
    {
        // ★死亡中は絶対に触らない（割り込み防止）
        if (IsDead || _isInDeathSequence) return;

        if (_animCtrl != null)
        {
            _animCtrl.SetGuard(isGuarding);
        }

        if (_isGuardHeld == isGuarding) return;
        _isGuardHeld = isGuarding;

        if (isGuarding)
        {
            CaptureGuardBasePosition();
            RaiseGuardVisualNow();
            ExtendGuardVisualHold();
        }
        else
        {
            ExtendGuardVisualHold();
        }
    }

    private void CaptureGuardBasePosition()
    {
        if (_characterRoot == null) return;

        _guardBaseLocalPos = _characterRoot.localPosition;

        if (_isGuardVisualRaised)
        {
            _guardBaseLocalPos.y -= _guardVisualYOffset;
        }
    }

    private void RaiseGuardVisualNow()
    {
        if (_characterRoot == null || Mathf.Approximately(_guardVisualYOffset, 0f)) return;
        if (_isGuardVisualRaised) return;

        var p = _guardBaseLocalPos;
        p.y += _guardVisualYOffset;
        _characterRoot.localPosition = p;
        _isGuardVisualRaised = true;
    }

    private void ExtendGuardVisualHold()
    {
        if (_characterRoot == null || Mathf.Approximately(_guardVisualYOffset, 0f)) return;

        float len = (_animCtrl != null) ? _animCtrl.GetGuardAnimationLength() : 0.5f;
        _guardHoldUntilTime = Mathf.Max(_guardHoldUntilTime, Time.time + len);

        if (_guardVisualCoroutine == null)
        {
            _guardVisualCoroutine = StartCoroutine(GuardVisualHoldCoroutine());
        }
    }

    private IEnumerator GuardVisualHoldCoroutine()
    {
        while (true)
        {
            // ★死亡中はガード見た目処理も止める（座標が変になるの防止）
            if (IsDead || _isInDeathSequence)
            {
                _guardVisualCoroutine = null;
                yield break;
            }

            if (_isGuardHeld)
            {
                RaiseGuardVisualNow();
                yield return null;
                continue;
            }

            if (Time.time < _guardHoldUntilTime)
            {
                yield return null;
                continue;
            }

            if (_characterRoot != null)
            {
                _characterRoot.localPosition = _guardBaseLocalPos;
            }

            _isGuardVisualRaised = false;
            _guardVisualCoroutine = null;
            yield break;
        }
    }

    /// <summary>
    /// 死亡アニメーションを再生する（割り込みを完全停止して再生しきる）
    /// </summary>
    public void PlayDeadAnimation()
    {
        if (IsDead) return;

        IsDead = true;
        _isInDeathSequence = true;

        // 死亡アニメーションの割り込み元を止める（PlayerDeathController と同じ思想）
        if (_moveCtrl != null) _moveCtrl.enabled = false;
        if (_combatCtrl != null) _combatCtrl.enabled = false;
        if (_animCtrl != null) _animCtrl.SetDeathMode(true);

        // ガード見た目も止める
        _isGuardHeld = false;
        if (_guardVisualCoroutine != null)
        {
            StopCoroutine(_guardVisualCoroutine);
            _guardVisualCoroutine = null;
        }

        // Collider off
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        if (_animator == null)
        {
            if (_logDeathDebug) Debug.LogWarning("[DeathDebug] Animator not found.", this);
            _isInDeathSequence = false;
            return;
        }

        // 死亡開始時に “割り込みパラメータ” を全部落とす
        _animCtrl?.ResetForDeath();

        // Deadトリガー投入
        _animator.SetTrigger("IsDead");

        // デバッグログ
        if (_logDeathDebug)
        {
            StartCoroutine(LogAnimatorStatesForSeconds(_animator, _deathLogDuration));
        }

        // ★「最後まで再生してその場に残す」なら PlayDeadAndFreeze を使うのが安全
        StartCoroutine(PlayDeadAndFreeze());
    }

    private IEnumerator LogAnimatorStatesForSeconds(Animator anim, float seconds)
    {
        int lastHash = 0;
        float lastNorm = -1f;

        float end = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < end && anim != null)
        {
            var st = anim.GetCurrentAnimatorStateInfo(0);

            bool stateChanged = st.shortNameHash != lastHash;
            bool jumpedBack = lastNorm >= 0f && st.normalizedTime + 0.05f < lastNorm;

            if (stateChanged || jumpedBack)
            {
                Debug.Log(
                    $"[DeathDebug] frame={Time.frameCount} hash={st.shortNameHash} norm={st.normalizedTime:F2} len={st.length:F2} " +
                    $"tagDead={st.IsTag("Dead")} nameDead={st.IsName("Dead")}",
                    this
                );

                lastHash = st.shortNameHash;
                lastNorm = st.normalizedTime;
            }
            else
            {
                lastNorm = st.normalizedTime;
            }

            yield return null;
        }

        Debug.Log($"[DeathDebug] Animator logging finished. frame={Time.frameCount}", this);
    }

    public IEnumerator PlayDeadAndFreeze()
    {
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        if (_animator == null) yield break;

        // Deadステートに入るのを待つ
        yield return null;

        float t = 0.5f;
        while (t > 0f)
        {
            var st = _animator.GetCurrentAnimatorStateInfo(0);
            if (st.IsTag("Dead") || st.IsName("Dead")) break;
            t -= Time.deltaTime;
            yield return null;
        }

        var state = _animator.GetCurrentAnimatorStateInfo(0);
        float remain = Mathf.Max(0.0f, (1f - state.normalizedTime) * state.length);
        if (remain <= 0.01f) remain = 1.0f;

        yield return new WaitForSecondsRealtime(remain);

        _animator.enabled = false;
        _isInDeathSequence = false;
    }
}
