using System.Collections;
using UnityEngine;

public class EnemyAttackCommand : ICommand
{
    public PlayerRuntime PlayerTarget { get; }
    public EnemyModel Attacker { get; }
    private EnemyAttackDamage calculator = new EnemyAttackDamage();
    private EnemyController _enemyController;
    private PlayerController _playerController;
    private PlayerStatusUIController _playerStatusUIController;

    public EnemyAttackCommand(PlayerRuntime player, EnemyModel enemy,
                              EnemyController enemyController,
                              PlayerController playerController,
                              PlayerStatusUIController playerStatusUIController)
    {
        this.PlayerTarget = player;
        this.Attacker = enemy;
        _enemyController = enemyController;
        _playerStatusUIController = playerStatusUIController;
        _playerController = playerController;
    }

    /// <summary>
    /// 敵の攻撃アニメーションを再生し、終わるまで待機する
    /// (ダメージ処理はここでは行わない)
    /// </summary>
    public IEnumerator Do()
    {
        EnemyAttackType attackType = Attacker.AttackType;
        bool isMelee = (attackType == EnemyAttackType.Melee);
        if (isMelee)
        {
            if (_enemyController != null && _playerController != null)
            {
                yield return _enemyController.MoveToTarget(_playerController.transform.position);
            }
        }
        //Debug.Log($"攻撃実行: Enemy='{Attacker.EnemyID}' が Player='{PlayerTarget.PlayerModel.PlayerID}' に攻撃開始！");
        float attackAnimTime = 0.5f;
        if (_enemyController != null)
        {
            attackAnimTime = _enemyController.PlayRandomAttackAnimation();
        }

        // アニメーションが終了するまで待機する
        yield return new WaitForSeconds(attackAnimTime);
        if (isMelee)
        {
            if (_enemyController != null)
            {
                yield return _enemyController.ReturnToOriginalPosition();
            }
        }
    }

    /// <summary>
    ///  EnemyTurn から呼び出される実際のダメージ処理
    /// </summary>
    public void ApplyDamageAfterJudgement()
    {
        if (PlayerTarget == null || PlayerTarget.HPHandler == null) return;
        float damage = calculator.CalculateFinalDamage(
            Attacker,
            PlayerTarget.PlayerModel
        );

        if (_enemyController != null && PlayerTarget.PlayerController != null)
        {
            Vector3 targetPos = PlayerTarget.PlayerController.transform.position;
            _enemyController.PlayAttackEffect(Attacker.AttackType, targetPos);
        }
        SoundManager.Instance.PlaySE(SEType.damagedPlayer);
        PlayerTarget.HPHandler.TakeDamage(damage);
        _playerStatusUIController.UpdateHP(PlayerTarget.CurrentHP);

        Debug.Log($"[EnemyAttackCardCommand] {PlayerTarget.PlayerModel.PlayerName} に {damage:F2} ダメージを与えた。残りHP: {PlayerTarget.CurrentHP:F2}");
    }

    public bool Undo()
    {
        Debug.Log("[EnemyAttackCardCommand] Undo not implemented.");
        return false;
    }
}