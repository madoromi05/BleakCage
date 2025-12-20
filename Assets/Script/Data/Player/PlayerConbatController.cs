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
    /// 攻撃シーケンスの実行
    /// </summary>
    public IEnumerator ExecuteAttackSequence(CardModel cardModel, WeaponRuntime weaponRuntime, Transform target)
    {
        // 1. 武器生成
        GameObject weaponObj = CreateWeapon(weaponRuntime, cardModel.WeaponHand);

        // 2. 移動 (近接のみ)
        if (cardModel.IsMelee)
        {
            yield return moveCtrl.MoveToTarget(target.position);
        }

        bool isHitProcessed = false;

        // 攻撃ヒット時の処理（エフェクト表示 & イベント発火）
        Action hitHandler = () =>
        {
            SpawnEffect(cardModel, target);
            OnAttackHitTriggered?.Invoke();
            isHitProcessed = true;
        };

        // アニメーションイベント購読
        animCtrl.OnAttackHitTriggered += hitHandler;

        // 3. アニメーション再生
        AnimationClip clip = cardModel.AttackAnimation;
        if (clip != null)
        {
            animCtrl.PlayAttackAnimation(clip);
            yield return new WaitForSeconds(clip.length);

            // 安全策：イベントが来なかった場合の待機
            float waitTimer = 0f;
            while (!isHitProcessed && waitTimer < 2.0f)
            {
                waitTimer += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            Debug.LogWarning($"Card {cardModel.Name} に AttackAnimation が設定されていません。");
            hitHandler.Invoke();
            yield return new WaitForSeconds(0.5f);
        }

        // 購読解除
        animCtrl.OnAttackHitTriggered -= hitHandler;

        // 4. 武器削除
        if (weaponObj != null) Destroy(weaponObj);

        // 5. 元の位置に戻る (近接のみ)
        if (cardModel.IsMelee)
        {
            yield return moveCtrl.ReturnToOriginalPosition();
        }
    }

    // エフェクト生成処理
    private void SpawnEffect(CardModel cardModel, Transform target)
    {
        if (cardModel.EffectPrefab != null && target != null)
        {
            Debug.Log($"Combat: Instantiate Effect at {target.position}");

            // EffectOffset を加算して位置を決定
            Vector3 effectPos = target.position + Vector3.up * 2.0f;

            // プレハブの回転を維持して生成
            GameObject effect = Instantiate(cardModel.EffectPrefab, effectPos, cardModel.EffectPrefab.transform.rotation);
            Destroy(effect, 2.0f);
        }
    }

    // 武器生成処理
    private GameObject CreateWeapon(WeaponRuntime weaponRuntime, HandPosition handPos)
    {
        if (weaponRuntime?.Prefab == null) return null;

        Transform socket = (handPos == HandPosition.RightHand) ? rightHandSocket : leftHandSocket;
        if (socket == null) return null;

        GameObject obj = Instantiate(weaponRuntime.Prefab, socket);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        return obj;
    }
}