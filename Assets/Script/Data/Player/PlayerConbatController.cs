using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// 攻撃の実行、武器の生成、エフェクトの処理を担当するクラス
/// </summary>
public class PlayerCombatController : MonoBehaviour
{
    private PlayerAnimationController animCtrl;
    private PlayerMovementController moveCtrl;

    private Transform rightHandSocket;
    private Transform leftHandSocket;

    private GameObject _currentMainWeaponObj;
    private WeaponRuntime _currentMainWeaponRuntime;

    public event Action OnAttackHitTriggered;

    /// <summary>
    /// 必要なコンポーネントとソケット情報を受け取る
    /// </summary>
    public void Init(PlayerAnimationController anim, PlayerMovementController move, Transform rightHand, Transform leftHand)
    {
        this.animCtrl = anim;
        this.moveCtrl = move;
        this.rightHandSocket = rightHand;
        this.leftHandSocket = leftHand;
    }

    /// <summary>
    /// 戦闘開始時にメイン武器を装備するメソッド
    /// </summary>
    public void EquipMainWeapon(WeaponRuntime weaponRuntime)
    {
        // 既に持っているなら消す
        if (_currentMainWeaponObj != null) Destroy(_currentMainWeaponObj);

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

        // 使う武器が「今装備しているもの」と同じならそれを使う
        if (_currentMainWeaponRuntime != null && weaponRuntime.Prefab == _currentMainWeaponRuntime.Prefab)
        {
            attackWeaponObj = _currentMainWeaponObj;
            isTemporaryWeapon = false;
        }
        else
        {
            // 違う武器（武器カードなど）なら、一時的に生成する
            if (_currentMainWeaponObj != null) _currentMainWeaponObj.SetActive(false);

            attackWeaponObj = CreateWeapon(weaponRuntime, weaponRuntime.Model.HoldHandType); 
            isTemporaryWeapon = true;
        }

        // 移動 (近接のみ)
        if (cardModel.IsMelee)
        {
            yield return moveCtrl.MoveToTarget(target.position);
        }

        bool isHitProcessed = false;
        Action hitHandler = () =>
        {
            PlayEffect(cardModel.EffectPrefab, target.position + Vector3.up * 2.0f);
            OnAttackHitTriggered?.Invoke();
            isHitProcessed = true;
        };

        animCtrl.OnAttackHitTriggered += hitHandler;

        // アニメーション再生
        AnimationClip clip = cardModel.AttackAnimation;
        if (clip != null)
        {
            animCtrl.PlayAttackAnimation(clip);
            yield return new WaitForSeconds(clip.length);

            float waitTimer = 0f;
            while (!isHitProcessed && waitTimer < 2.0f)
            {
                waitTimer += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            hitHandler.Invoke();
            yield return new WaitForSeconds(0.5f);
        }

        animCtrl.OnAttackHitTriggered -= hitHandler;

        if (isTemporaryWeapon)
        {
            if (attackWeaponObj != null) Destroy(attackWeaponObj);
            if (_currentMainWeaponObj != null) _currentMainWeaponObj.SetActive(true);
        }

        if (cardModel.IsMelee)
        {
            yield return moveCtrl.ReturnToOriginalPosition();
        }
    }
    /// <summary>
    /// 支援・回復時の演出
    /// ★修正: PlayAttackAnimation を再利用して、回復モーションを再生する
    /// </summary>
    public IEnumerator ExecuteSupportEffect(CardModel cardModel)
    {
        if (cardModel.EffectPrefab != null)
        {
            PlayEffect(cardModel.EffectPrefab, transform.position + Vector3.up * 2.0f);
        }
        AnimationClip clip = cardModel.AttackAnimation;

        if (clip != null && animCtrl != null)
        {
            // 回復モーションをセットして再生！
            animCtrl.PlayAttackAnimation(clip);

            // アニメーションの長さ分待機
            yield return new WaitForSeconds(clip.length);
        }
        else
        {
            // クリップがない場合は一瞬待つだけ
            yield return new WaitForSeconds(0.5f);
        }
    }

    /// <summary>
    /// 【共通メソッド】エフェクトを生成して自動で削除する
    /// </summary>
    private void PlayEffect(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return;
        GameObject effect = Instantiate(prefab, position, prefab.transform.rotation);
        Destroy(effect, 2.0f);
    }

    // 武器生成処理
    private GameObject CreateWeapon(WeaponRuntime weaponRuntime, HandPosition handPos)
    {
        if (weaponRuntime?.Prefab == null) return null;

        Transform socket = (handPos == HandPosition.RightHand) ? rightHandSocket : leftHandSocket;
        if (socket == null) return null;

        GameObject obj = Instantiate(weaponRuntime.Prefab, socket);
        obj.name = weaponRuntime.Prefab.name;
        obj.transform.localPosition = weaponRuntime.Prefab.transform.localPosition;
        obj.transform.localRotation = weaponRuntime.Prefab.transform.localRotation;
        obj.transform.localScale = weaponRuntime.Prefab.transform.localScale;
        return obj;
    }
}