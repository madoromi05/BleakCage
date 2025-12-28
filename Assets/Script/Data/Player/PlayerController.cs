using UnityEngine;
using System.Collections;
using System;

[RequireComponent(typeof(PlayerAnimationController))]
[RequireComponent(typeof(PlayerMovementController))]
[RequireComponent(typeof(PlayerCombatController))]
public class PlayerController : MonoBehaviour
{
    [Header("Death Debug")]
    [SerializeField] private bool logDeathDebug = false;
    [SerializeField] private float deathLogDuration = 3.0f;


    public void SetStatusUI(PlayerStatusUIController ui) => this.statusUI = ui;
    public bool IsDead { get; private set; } = false;

    private PlayerAnimationController animCtrl;
    private PlayerMovementController moveCtrl;
    private PlayerCombatController combatCtrl;
    private PlayerStatusUIController statusUI;

    private Transform _characterRoot;
    private float _guardVisualYOffset = 0f;
    private bool _isGuardVisualRaised = false;
    private Coroutine _guardVisualCoroutine;
    private float _guardHoldUntilTime = 0f;
    private bool _isGuardHeld = false;
    private Vector3 _guardBaseLocalPos; // ガード開始時の基準位置
    private Animator _anim;

    // イベント中継
    public event Action OnAttackHitTriggered
    {
        add => combatCtrl.OnAttackHitTriggered += value;
        remove => combatCtrl.OnAttackHitTriggered -= value;
    }

    private void Awake()
    {
        animCtrl = GetComponent<PlayerAnimationController>();
        moveCtrl = GetComponent<PlayerMovementController>();
        combatCtrl = GetComponent<PlayerCombatController>();
    }

    public void Init(PlayerModel model)
    {
        moveCtrl.Init(transform.localPosition);
        _guardVisualYOffset = model != null ? model.GuardVisualYOffset : 0f;

        if (model.CharacterPrefab != null)
        {
            GameObject instance = Instantiate(model.CharacterPrefab, transform);
            _characterRoot = instance.transform;

            // Prefabのローカル座標・スケールを適用
            instance.transform.localPosition = model.CharacterPrefab.transform.localPosition;
            instance.transform.localScale = model.CharacterPrefab.transform.localScale;

            // 回転の適用: Prefabの回転 × Entityで設定した初期回転(InitialRotation)
            Quaternion prefabRot = model.CharacterPrefab.transform.localRotation;
            Quaternion adjustRot = Quaternion.Euler(model.InitialRotation);
            instance.transform.localRotation = prefabRot * adjustRot;

            _guardBaseLocalPos = _characterRoot.localPosition;
            _isGuardVisualRaised = false;

            Animator anim = instance.GetComponent<Animator>();
            CharacterBoneHolder boneHolder = instance.GetComponent<CharacterBoneHolder>();

            Transform rightHandSocket = boneHolder != null ? boneHolder.RightHandTransform : null;
            Transform leftHandSocket = boneHolder != null ? boneHolder.LeftHandTransform : null;

            if (anim != null)
            {
                animCtrl.Init(anim);
            }
            combatCtrl.Init(animCtrl, moveCtrl, rightHandSocket, leftHandSocket);
        }
    }

    public void SetInitialWeapon(WeaponRuntime weaponRuntime)
    {
        if (combatCtrl != null && weaponRuntime != null)
        {
            combatCtrl.EquipMainWeapon(weaponRuntime);
        }
    }

    public IEnumerator AttackSequence(CardModel cardModel, WeaponRuntime weaponRuntime, Transform target)
    {
        yield return combatCtrl.ExecuteAttackSequence(cardModel, weaponRuntime, target);
    }

    public IEnumerator SupportEffect(CardModel cardModel)
    {
        yield return combatCtrl.ExecuteSupportEffect(cardModel);
    }

    public void SetGuardAnimation(bool isGuarding)
    {
        // アニメはあくまで見た目（無くても座標上げはしたい）
        if (animCtrl != null)
            animCtrl.SetGuard(isGuarding);
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

        Debug.Log(
       $"[CaptureBase] frame={Time.frameCount} " +
       $"before base={_guardBaseLocalPos} current={_characterRoot.localPosition} raised={_isGuardVisualRaised}"
   );
        _guardBaseLocalPos = _characterRoot.localPosition;

        if (_isGuardVisualRaised)
        {
            _guardBaseLocalPos.y -= _guardVisualYOffset;
            Debug.Log($"[CaptureBase] adjusted base={_guardBaseLocalPos}");
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

        float len = (animCtrl != null) ? animCtrl.GetGuardAnimationLength() : 0.5f;
        // “いまからlen秒”まで保持。すでに保持中なら延長。
        _guardHoldUntilTime = Mathf.Max(_guardHoldUntilTime, Time.time + len);

        if (_guardVisualCoroutine == null)
            _guardVisualCoroutine = StartCoroutine(GuardVisualHoldCoroutine());
    }

    private IEnumerator GuardVisualHoldCoroutine()
    {
        while (true)
        {
            // ガード入力中は“絶対に下げない”
            if (_isGuardHeld)
            {
                // 念のため上がってなければ上げる（ここでも二重上げはしない）
                RaiseGuardVisualNow();
                yield return null;
                continue;
            }

            // 入力解除後：保持時間が切れるまで待つ
            if (Time.time < _guardHoldUntilTime)
            {
                yield return null;
                continue;
            }

            // 時間切れで戻す
            if (_characterRoot != null)
                _characterRoot.localPosition = _guardBaseLocalPos;

            _isGuardVisualRaised = false;
            _guardVisualCoroutine = null;
            yield break;
        }
    }


    /// <summary>
    /// 死亡アニメーションを再生する（ログ付き）
    /// </summary>
    public void PlayDeadAnimation()
    {
        if (IsDead) return;
        IsDead = true;

        if (logDeathDebug)
        {
            Debug.Log($"[DeathDebug] PlayDeadAnimation START frame={Time.frameCount} timeScale={Time.timeScale}", this);
        }

        if (_anim == null) _anim = GetComponentInChildren<Animator>();

        if (_anim != null)
        {
            // Deadトリガー投入
            _anim.SetTrigger("IsDead");

            if (logDeathDebug)
            {
                var st = _anim.GetCurrentAnimatorStateInfo(0);
                Debug.Log($"[DeathDebug] Trigger set. currentState={st.shortNameHash} normalized={st.normalizedTime:F2} length={st.length:F2}", this);
            }

            // ★ここが重要：死亡中の状態遷移を追跡する
            StartCoroutine(LogAnimatorStatesForSeconds(_anim, deathLogDuration));
        }
        else
        {
            if (logDeathDebug) Debug.LogWarning("[DeathDebug] Animator not found.", this);
        }

        // ガード見た目は戻す（これはOK）
        if (_characterRoot != null)
        {
            _characterRoot.localPosition = _guardBaseLocalPos;
            _isGuardVisualRaised = false;
        }

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    /// <summary>
    /// 数秒間、Animatorのステート変化を毎フレームログ
    /// </summary>
    private IEnumerator LogAnimatorStatesForSeconds(Animator anim, float seconds)
    {
        int lastHash = 0;
        float lastNorm = -1f;

        float end = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < end && anim != null)
        {
            var st = anim.GetCurrentAnimatorStateInfo(0);

            // ステートが変わった or normalizedTimeが急に戻った（割り込み）ときだけ出す
            bool stateChanged = st.shortNameHash != lastHash;
            bool jumpedBack = lastNorm >= 0f && st.normalizedTime + 0.05f < lastNorm;

            if (stateChanged || jumpedBack)
            {
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
        if (_anim == null) _anim = GetComponentInChildren<Animator>();
        if (_anim == null) yield break;

        _anim.SetTrigger("IsDead");
        yield return null;

        float t = 0.5f;
        while (t > 0f)
        {
            var st = _anim.GetCurrentAnimatorStateInfo(0);
            if (st.IsTag("Dead") || st.IsName("Dead")) break;
            t -= Time.deltaTime;
            yield return null;
        }

        var state = _anim.GetCurrentAnimatorStateInfo(0);
        float remain = Mathf.Max(0.0f, (1f - state.normalizedTime) * state.length);
        if (remain <= 0.01f) remain = 1.0f;

        // ★timeScale=0対策（ここが重要）
        yield return new WaitForSecondsRealtime(remain);

        _anim.enabled = false;
    }

}