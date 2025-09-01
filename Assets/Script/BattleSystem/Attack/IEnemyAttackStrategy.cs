/// <summary>
/// 敵の攻撃の「方法」を定義するアルゴリズムの共通インターフェース
/// </summary>
public interface IEnemyAttackStrategy
{
    // 計算に必要な情報を渡せるよう
    float CalculateFinalDamage(EnemyModel enemyModel, PlayerModel playerModel);
}
