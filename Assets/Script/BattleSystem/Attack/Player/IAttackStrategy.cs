/// <summary>
/// 最も一般的な攻撃方法を定義するインターフェース
/// </summary>
public interface IAttackStrategy
{
    // 計算に必要な情報を渡せるよう
    float CalculateFinalDamage(PlayerRuntime playerRuntime, WeaponRuntime weaponRuntime, CardRuntime cardRuntime, EnemyModel enemyModel);
}
