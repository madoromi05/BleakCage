using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// プレイヤーの移動、戦闘、アニメーションを統合管理するメインコントローラー
/// </summary>
[RequireComponent(typeof(PlayerAnimationController))]
[RequireComponent(typeof(PlayerMovementController))]
[RequireComponent(typeof(PlayerCombatController))]
public class PlayerController : MonoBehaviour
{
    [Header("Death Debug")]
    [SerializeField] private bool _logDeathDebug = false;

    public bool IsDead { get; private set; } = false;

    private PlayerAnimationController _animCtrl;
    private PlayerMovementController _moveCtrl;
    private PlayerCombatController _combatCtrl;
    private PlayerStatusUIController _statusUi;

    // --- ビジュアル・ガード制御用変数 ---
    private Transform _characterRoot;
    private float _guardVisualYOffset = 0f;
    private bool _isGuardVisualRaised = false;
    private Coroutine _guardVisualCoroutine;
    private float _guardHoldUntilTime = 0f;
    private bool _isGuardHeld = false;
    private Vector3 _guardBaseLocalPos;
    private Animator _animator;

    private bool _isInDeathSequence = false;

    public void SetStatusUI(PlayerStatusUIController ui) => _statusUi = ui;

    /// <summary>
    /// 戦闘用コンポーネントからの攻撃ヒットイベントを中継
    /// </summary>
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

    /// <summary>
    /// モデルデータに基づきプレイヤーを初期化し、キャラクターモデルを生成します
    /// </summary>
    public void Init(PlayerModel model)
    {
        _moveCtrl.Init(transform.localPosition);
        _guardVisualYOffset = model != null ? model.GuardVisualYOffset : 0f;

        if (model.CharacterPrefab != null)
        {
            // モデルの生成とトランスフォーム設定
            GameObject instance = Instantiate(model.CharacterPrefab, transform);
            _characterRoot = instance.transform;

            instance.transform.localPosition = model.CharacterPrefab.transform.localPosition;
            instance.transform.localScale = model.CharacterPrefab.transform.localScale;

            // 回転補正の計算（プレハブ本来の回転 × 初期設定の回転）
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

    /// <summary>
    /// 初期装備を設定します
    /// </summary>
    public void SetInitialWeapon(WeaponRuntime weaponRuntime)
    {
        if (_combatCtrl != null && weaponRuntime != null)
        {
            _combatCtrl.EquipMainWeapon(weaponRuntime);
        }
    }

    /// <summary>
    /// 攻撃シーケンスを開始します
    /// </summary>
    public IEnumerator AttackSequence(CardModel cardModel, WeaponRuntime weaponRuntime, Transform target)
    {
        if (IsDead) yield break;
        yield return _combatCtrl.ExecuteAttackSequence(cardModel, weaponRuntime, target);
    }

    /// <summary>
    /// 補助効果を発動します
    /// </summary>
    public IEnumerator SupportEffect(CardModel cardModel)
    {
        if (IsDead) yield break;
        yield return _combatCtrl.ExecuteSupportEffect(cardModel);
    }

    /// <summary>
    /// ガードアニメーションとビジュアル（位置のオフセット）を設定
    /// </summary>
    public void SetGuardAnimation(bool isGuarding)
    {
        if (IsDead || _isInDeathSequence) return;

        if (_animCtrl != null)
        {
            _animCtrl.SetGuard(isGuarding);
        }

        if (_isGuardHeld == isGuarding) return;
        _isGuardHeld = isGuarding;

        if (isGuarding)
        {
            CaptureGuardBasePosition();  // ガード開始時の基準位置を記録
            RaiseGuardVisualNow();       // モデルを浮かす（またはオフセットさせる）演出
            ExtendGuardVisualHold();     // 維持時間の更新
        }
        else
        {
            ExtendGuardVisualHold();     // ガードを解いてもアニメーションが終わるまでは維持
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

        // アニメーションクリップの長さを取得して維持時間を計算
        float len = (_animCtrl != null) ? _animCtrl.GetGuardAnimationLength() : 0.5f;
        _guardHoldUntilTime = Mathf.Max(_guardHoldUntilTime, Time.time + len);

        if (_guardVisualCoroutine == null)
        {
            _guardVisualCoroutine = StartCoroutine(GuardVisualHoldCoroutine());
        }
    }

    /// <summary>
    /// ガード演出（位置オフセット）を管理するコルーチン
    /// </summary>
    private IEnumerator GuardVisualHoldCoroutine()
    {
        while (true)
        {
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
    /// 死亡処理を開始し、他のアクションを強制停止します
    /// </summary>
    public void PlayDeadAnimation()
    {
        if (IsDead) return;

        IsDead = true;
        _isInDeathSequence = true;

        // 他の制御コンポーネントを停止して入力を遮断
        if (_moveCtrl != null) _moveCtrl.enabled = false;
        if (_combatCtrl != null) _combatCtrl.enabled = false;
        if (_animCtrl != null) _animCtrl.SetDeathMode(true);

        // ガード演出の停止
        _isGuardHeld = false;
        if (_guardVisualCoroutine != null)
        {
            StopCoroutine(_guardVisualCoroutine);
            _guardVisualCoroutine = null;
        }

        // 当たり判定の無効化
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        if (_animator == null)
        {
            if (_logDeathDebug) DebugCostom.LogWarning("[DeathDebug] Animator not found.", this);
            _isInDeathSequence = false;
            return;
        }

        // 再生中のアニメーションをリセットし、死亡ステートへ遷移
        _animCtrl?.ResetForDeath();
        _animator.SetTrigger("IsDead");

        // アニメーション終了後にフリーズさせる処理を開始
        StartCoroutine(PlayDeadAndFreeze());
    }

    public IEnumerator PlayDeadAndFreeze()
    {
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        if (_animator == null) yield break;

        yield return null;

        float timeout = 0.5f;
        while (timeout > 0f)
        {
            var st = _animator.GetCurrentAnimatorStateInfo(0);
            if (st.IsTag("Dead") || st.IsName("Dead")) break;
            timeout -= Time.deltaTime;
            yield return null;
        }

        var state = _animator.GetCurrentAnimatorStateInfo(0);
        float remain = Mathf.Max(0.0f, (1f - state.normalizedTime) * state.length);
        if (remain <= 0.01f) remain = 1.0f;

        // 終了まで待機してAnimatorをオフにする（ポーズ状態で残す）
        yield return new WaitForSecondsRealtime(remain);

        _animator.enabled = false;
        _isInDeathSequence = false;
    }
}