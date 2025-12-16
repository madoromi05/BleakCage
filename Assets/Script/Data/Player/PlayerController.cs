using UnityEngine;
using System.Collections;
using System;

[RequireComponent(typeof(PlayerAnimationController))]
[RequireComponent(typeof(PlayerMovementController))]
public class PlayerController : MonoBehaviour
{
    public void SetStatusUI(PlayerStatusUIController ui) => this.statusUI = ui;
    // コンポーネント参照
    private PlayerAnimationController animCtrl;
    private PlayerMovementController moveCtrl;
    private PlayerStatusUIController statusUI;

    // イベント中継
    public event Action OnAttackHitTriggered;

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

            rightHandSocket = boneHolder.RightHandTransform;
            leftHandSocket = boneHolder.LeftHandTransform;

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

        bool isHitProcessed = false;

        Action hitHandler = () =>
        {
            // 近接攻撃の場合
            if (cardModel.IsMelee)
            {
                OnAttackHitTriggered?.Invoke();
            }
            // 遠距離攻撃
            else
            {
                if (cardModel.ProjectilePrefab != null)
                {
                    SpawnAndFireProjectile(cardModel.ProjectilePrefab, targetEnemy, () =>
                    {
                        OnAttackHitTriggered?.Invoke();
                    });
                }
                else
                {
                    // 弾の設定がない
                    Debug.LogError($"Card {cardModel.Name} は遠距離ですが ProjectilePrefab がありません。");
                }
            }
            isHitProcessed = true;
        };

        animCtrl.OnAttackHitTriggered += hitHandler;

        // アニメーション
        AnimationClip clip = cardModel.AttackAnimation;
        if (clip != null)
        {
            animCtrl.PlayAttackAnimation(clip);
            yield return new WaitForSeconds(clip.length);
            //遠距離攻撃が終わってなかったら2s待つ
            float waitTimer = 0f;
            while (!isHitProcessed && waitTimer < 2.0f)
            {
                waitTimer += Time.deltaTime;
                yield return null;
            }
        } else {
            Debug.LogError($"Card {cardModel.Name} に AttackAnimation が設定されていません。");
        }

        animCtrl.OnAttackHitTriggered -= hitHandler;

        // 武器削除
        if (weaponObj != null) Destroy(weaponObj);

        // 戻る
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

    // 弾を生成、発射する関数
    private void SpawnAndFireProjectile(ProjectileMove prefab, Transform target, Action onHit)
    {
        Vector3 spawnPosition = transform.position + Vector3.up * 1.2f + transform.forward * 0.5f;
        ProjectileMove projectile = Instantiate(prefab, spawnPosition, Quaternion.identity);

        if (projectile != null)
        {
            projectile.Fire(target, onHit);
        }
    }
}