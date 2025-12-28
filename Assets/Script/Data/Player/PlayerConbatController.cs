using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// 攻撃の実行、武器の生成、エフェクトの処理を担当するクラス
/// </summary>
public class PlayerCombatController : MonoBehaviour
{
    public event Action OnAttackHitTriggered;


    private const float DefaultHitWaitTimeoutSeconds = 2.0f;
    private const float DefaultNoClipWaitSeconds = 0.5f;
    private const float EffectSpawnHeight = 2.0f;

    private PlayerAnimationController _animCtrl;
    private PlayerMovementController _moveCtrl;

    private Transform _rightHandSocket;
    private Transform _leftHandSocket;

    private GameObject _currentMainWeaponObj;
    private WeaponRuntime _currentMainWeaponRuntime;

    /// <summary>
    /// 必要なコンポーネントとソケット情報を受け取る
    /// </summary>
    public void Init(PlayerAnimationController anim, PlayerMovementController move, Transform rightHand, Transform leftHand)
    {
        _animCtrl = anim;
        _moveCtrl = move;
        _rightHandSocket = rightHand;
        _leftHandSocket = leftHand;
    }

    /// <summary>
    /// 戦闘開始時にメイン武器を装備するメソッド
    /// </summary>
    public void EquipMainWeapon(WeaponRuntime weaponRuntime)
    {
        if (_currentMainWeaponObj != null)
        {
            Destroy(_currentMainWeaponObj);
        }

        _currentMainWeaponRuntime = weaponRuntime;
        _currentMainWeaponObj = CreateWeapon(weaponRuntime, weaponRuntime.Model.HoldHandType);
    }


    /// <summary>
    /// 攻撃シーケンスの実行
    /// </summary>
    public IEnumerator ExecuteAttackSequence(CardModel cardModel, WeaponRuntime weaponRuntime, Transform target)
    {
        GameObject attackWeaponObj = null;
        bool isTemporaryWeapon = false;

        if (_currentMainWeaponRuntime != null && weaponRuntime.Prefab == _currentMainWeaponRuntime.Prefab)
        {
            attackWeaponObj = _currentMainWeaponObj;
            isTemporaryWeapon = false;
        }
        else
        {
            if (_currentMainWeaponObj != null) _currentMainWeaponObj.SetActive(false);

            attackWeaponObj = CreateWeapon(weaponRuntime, weaponRuntime.Model.HoldHandType);
            isTemporaryWeapon = true;
        }

        if (cardModel.IsMelee && _moveCtrl != null && target != null)
        {
            yield return _moveCtrl.MoveToTarget(target.position);
        }

        bool isHitProcessed = false;

        Action hitHandler = () =>
        {
            Vector3 pos = (target != null) ? target.position : transform.position;
            pos += Vector3.up * EffectSpawnHeight;

            // 共通エフェクト生成（寿命自動）
            if (cardModel.EffectPrefab != null)
            {
                EffectSpawner.SpawnAndAutoDestroy(cardModel.EffectPrefab, pos, cardModel.EffectPrefab.transform.rotation);
            }

            OnAttackHitTriggered?.Invoke();
            isHitProcessed = true;
        };

        if (_animCtrl != null)
        {
            _animCtrl.OnAttackHitTriggered += hitHandler;
        }

        AnimationClip clip = cardModel.AttackAnimation;

        if (clip != null && _animCtrl != null)
        {
            _animCtrl.PlayAttackAnimation(clip);
            yield return new WaitForSeconds(clip.length);

            float waitTimer = 0f;
            while (!isHitProcessed && waitTimer < DefaultHitWaitTimeoutSeconds)
            {
                waitTimer += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            hitHandler.Invoke();
            yield return new WaitForSeconds(DefaultNoClipWaitSeconds);
        }

        if (_animCtrl != null)
        {
            _animCtrl.OnAttackHitTriggered -= hitHandler;
        }

        if (isTemporaryWeapon)
        {
            if (attackWeaponObj != null) Destroy(attackWeaponObj);
            if (_currentMainWeaponObj != null) _currentMainWeaponObj.SetActive(true);
        }

        if (cardModel.IsMelee && _moveCtrl != null)
        {
            yield return _moveCtrl.ReturnToOriginalPosition();
        }
    }

    /// <summary>
    /// 支援・回復時の演出
    /// </summary>
    public IEnumerator ExecuteSupportEffect(CardModel cardModel)
    {
        if (cardModel.EffectPrefab != null)
        {
            Vector3 pos = transform.position + Vector3.up * EffectSpawnHeight;
            EffectSpawner.SpawnAndAutoDestroy(cardModel.EffectPrefab, pos, cardModel.EffectPrefab.transform.rotation);
        }

        AnimationClip clip = cardModel.AttackAnimation;

        if (clip != null && _animCtrl != null)
        {
            _animCtrl.PlayAttackAnimation(clip);
            yield return new WaitForSeconds(clip.length);
        }
        else
        {
            yield return new WaitForSeconds(DefaultNoClipWaitSeconds);
        }
    }

    // 武器生成処理
    private GameObject CreateWeapon(WeaponRuntime weaponRuntime, HandPosition handPos)
    {
        if (weaponRuntime == null || weaponRuntime.Prefab == null) return null;

        Transform socket = (handPos == HandPosition.RightHand) ? _rightHandSocket : _leftHandSocket;
        if (socket == null) return null;

        GameObject obj = Instantiate(weaponRuntime.Prefab, socket);
        obj.name = weaponRuntime.Prefab.name;

        obj.transform.localPosition = weaponRuntime.Prefab.transform.localPosition;
        obj.transform.localRotation = weaponRuntime.Prefab.transform.localRotation;
        obj.transform.localScale = weaponRuntime.Prefab.transform.localScale;

        return obj;
    }
}