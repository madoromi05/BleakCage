/// <summary>
/// 最も一般的な攻撃方法を定義するインターフェース
/// </summary>
public interface IAttackStrategy
{
    float CalculateFinalDamage(PlayerRuntime playerRuntime, WeaponRuntime weaponRuntime, CardRuntime cardRuntime, EnemyModel enemyModel);
}
