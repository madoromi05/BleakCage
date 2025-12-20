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

        if (model.CharacterPrefab != null)
        {
            Quaternion rot = this.transform.rotation * Quaternion.Euler(model.InitialRotation);
            GameObject instance = Instantiate(model.CharacterPrefab, transform.position, rot, transform);

            Animator anim = instance.GetComponent<Animator>();
            CharacterBoneHolder boneHolder = instance.GetComponent<CharacterBoneHolder>();

            Transform rightHandSocket = boneHolder != null ? boneHolder.RightHandTransform : null;
            Transform leftHandSocket = boneHolder != null ? boneHolder.LeftHandTransform : null;

            if (anim != null)
            {
                animCtrl.Init(anim, model.PlayerAnimator);
            }
            combatCtrl.Init(animCtrl, moveCtrl, rightHandSocket, leftHandSocket);
        }
    }
    public IEnumerator AttackSequence(CardModel cardModel, WeaponRuntime weaponRuntime, Transform target)
    {
        yield return combatCtrl.ExecuteAttackSequence(cardModel, weaponRuntime, target);
    }

    public void SetGuardAnimation(bool isGuarding)
    {
        if (animCtrl != null)
        {
            animCtrl.SetGuard(isGuarding);
        }
    }
}