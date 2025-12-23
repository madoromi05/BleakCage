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
            GameObject instance = Instantiate(model.CharacterPrefab, transform);

            // Prefabのローカル座標・スケールを適用
            instance.transform.localPosition = model.CharacterPrefab.transform.localPosition;
            instance.transform.localScale = model.CharacterPrefab.transform.localScale;

            // 回転の適用: Prefabの回転 × Entityで設定した初期回転(InitialRotation)
            Quaternion prefabRot = model.CharacterPrefab.transform.localRotation;
            Quaternion adjustRot = Quaternion.Euler(model.InitialRotation);
            instance.transform.localRotation = prefabRot * adjustRot;

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
        if (animCtrl != null)
        {
            animCtrl.SetGuard(isGuarding);
        }
    }
}