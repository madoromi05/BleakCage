/// <summary>
/// 攻撃の「方法」を定義するアルゴリズムの共通インターフェース
/// </summary>
public interface IAttackStrategy
{
    // 計算に必要な情報を渡せるよう
    float CalculateFinalDamage(PlayerRuntime playerRuntime, WeaponRuntime weaponRuntime, CardRuntime cardRuntime, EnemyModel enemyModel);
}
