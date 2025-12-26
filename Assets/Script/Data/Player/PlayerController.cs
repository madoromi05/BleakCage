using UnityEngine;
using System.Collections;
using System;

[RequireComponent(typeof(PlayerAnimationController))]
[RequireComponent(typeof(PlayerMovementController))]
[RequireComponent(typeof(PlayerCombatController))]
public class PlayerController : MonoBehaviour
{
    public void SetStatusUI(PlayerStatusUIController ui) => this.statusUI = ui;

    // コンポーネント参照
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

        // “いま”の見た目位置をガード用ベースにする
        _guardBaseLocalPos = _characterRoot.localPosition;

        // もしすでに上げていた状態で呼ばれた場合に備えて、ベースも補正しておく
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
    /// 死亡アニメーションを再生する
    /// </summary>
    public void PlayDeadAnimation()
    {
        if (animCtrl != null)
        {
            GetComponentInChildren<Animator>()?.SetTrigger("IsDead");
        }
        if (_characterRoot != null)
        {
            _characterRoot.localPosition = _guardBaseLocalPos;
            _isGuardVisualRaised = false;
        }
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }
}