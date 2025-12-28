using System.Collections;
using UnityEngine;

public class EnemyAttackCommand : ICommand
{
    public PlayerRuntime PlayerTarget { get; }
    public EnemyModel Attacker { get; }
    public int HitCount { get; }

    private readonly EnemyAttackDamage _calculator = new EnemyAttackDamage();
    private readonly EnemyController _enemyController;
    private readonly PlayerController _playerController;
    private readonly PlayerStatusUIController _playerStatusUiController;

    public EnemyAttackCommand(
       PlayerRuntime playerTarget,
       EnemyModel attacker,
       EnemyController enemyController,
       PlayerController playerController,
       PlayerStatusUIController playerStatusUiController)
    {
        PlayerTarget = playerTarget;
        Attacker = attacker;
        _enemyController = enemyController;
        _playerController = playerController;
        _playerStatusUiController = playerStatusUiController;

        HitCount = (Attacker != null && Attacker.EnemyID == 4) ? 3 : 1;
    }

    /// <summary>
    /// 敵の攻撃アニメーションを再生し、終わるまで待機する
    /// (ダメージ処理はここでは行わない)
    /// </summary>
    public IEnumerator Do()
    {
        if (Attacker == null) yield break;

        bool isMelee = (Attacker.AttackType == EnemyAttackType.Melee);

        if (isMelee && _enemyController != null && _playerController != null)
        {
            yield return _enemyController.MoveToTarget(_playerController.transform.position);
        }

        float attackAnimTime = 0.5f;
        if (_enemyController != null)
        {
            attackAnimTime = _enemyController.PlayRandomAttackAnimation();
        }

        yield return new WaitForSeconds(attackAnimTime);

        if (isMelee && _enemyController != null)
        {
            yield return _enemyController.ReturnToOriginalPosition();
        }
    }


    /// <summary>
    ///  EnemyTurn から呼び出される実際のダメージ処理
    /// </summary>
    public void ApplyDamageAfterJudgement()
    {
        if (PlayerTarget == null || PlayerTarget.playerHpHandler == null) return;
        if (Attacker == null) return;

        float damage = _calculator.CalculateFinalDamage(Attacker, PlayerTarget.PlayerModel);

        if (_enemyController != null && PlayerTarget.PlayerController != null)
        {
            Vector3 targetPos = PlayerTarget.PlayerController.transform.position;
            _enemyController.PlayAttackEffect(Attacker.AttackType, targetPos);
        }

        SoundManager.Instance.PlaySE(SEType.damagedPlayer);

        PlayerTarget.playerHpHandler.TakeDamage(damage);
        if (_playerStatusUiController != null)
        {
            _playerStatusUiController.UpdateHP(PlayerTarget.CurrentHP);
        }
    }

    public bool Undo()
    {
        Debug.Log("[EnemyAttackCardCommand] Undo not implemented.");
        return false;
    }
}