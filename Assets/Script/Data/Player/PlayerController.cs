using UnityEngine;
using System.Collections;
using System;

[RequireComponent(typeof(PlayerAnimationController))]
[RequireComponent(typeof(PlayerMovementController))]
public class PlayerController : MonoBehaviour
{
    // コンポーネント参照
    private PlayerAnimationController animCtrl;
    private PlayerMovementController moveCtrl;
    private PlayerStatusUIController statusUI;

    // イベント中継 (Command側からはこれが見える)
    public event Action OnAttackHitTriggered
    {
        add => animCtrl.OnAttackHitTriggered += value;
        remove => animCtrl.OnAttackHitTriggered -= value;
    }

    private Transform rightHandSocket;
    private Transform leftHandSocket;

    private void Awake()
    {
        animCtrl = GetComponent<PlayerAnimationController>();
        moveCtrl = GetComponent<PlayerMovementController>();
    }

    public void Init(PlayerModel model)
    {
        moveCtrl.Init(transform.localPosition);

        // モデル生成 & Bone取得
        if (model.CharacterPrefab != null)
        {
            Quaternion rot = this.transform.rotation * Quaternion.Euler(model.InitialRotation);
            GameObject instance = Instantiate(model.CharacterPrefab, transform.position, rot, transform);

            Animator anim = instance.GetComponent<Animator>();
            CharacterBoneHolder boneHolder = instance.GetComponent<CharacterBoneHolder>();

            if (boneHolder != null)
            {
                rightHandSocket = boneHolder.RightHandTransform;
                leftHandSocket = boneHolder.LeftHandTransform;
            }

            // アニメコントローラー初期化
            animCtrl.Init(anim, model.PlayerAnimator);
        }
    }

    // コマンドから呼ばれるメイン処理
    public IEnumerator AttackSequence(CardModel cardModel, WeaponRuntime weaponRuntime, Transform targetEnemy)
    {
        //  武器生成
        GameObject weaponObj = CreateWeapon(weaponRuntime, cardModel.WeaponHand);

        // キャラ移動
        if (cardModel.IsMelee)
        {
            yield return moveCtrl.MoveToTarget(targetEnemy.position);
        }

        // アニメーション
        AnimationClip clip = cardModel.AttackAnimation;
        if (clip != null)
        {
            animCtrl.PlayAttackAnimation(clip);
            // クリップの長さ分待機
            yield return new WaitForSeconds(clip.length);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
            animCtrl.OnAnimationHit(); // フォールバック
        }

        // 4. 武器削除
        if (weaponObj != null) Destroy(weaponObj);

        // 5. 戻る
        if (cardModel.IsMelee)
        {
            yield return moveCtrl.ReturnToOriginalPosition();
        }
    }

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

    public void SetGuardAnimation(bool isGuarding)
    {
        if (animCtrl != null)
        {
            animCtrl.SetGuard(isGuarding);
        }

    }
    public float GetGuardAnimationLength()
    {
        return (animCtrl != null) ? animCtrl.GetGuardAnimationLength() : 0.5f;
    }

    // UI系
    public void SetStatusUI(PlayerStatusUIController ui) => this.statusUI = ui;
    public void UpdateHealthUI(float hp) => statusUI?.UpdateHP(hp);
}