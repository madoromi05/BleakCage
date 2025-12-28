using UnityEngine;

public static class EffectSpawner
{
    // 定数（MagicNumber排除）
    private const float DefaultFallbackLifetimeSeconds = 2.0f;

    /// <summary>
    /// エフェクトを生成して寿命後に破棄する（ParticleSystemがあれば寿命自動計算）
    /// </summary>
    public static void SpawnAndAutoDestroy(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return;

        GameObject effect = Object.Instantiate(prefab, position, rotation);

        float lifetimeSeconds = TryGetParticleLifetimeSeconds(effect);
        if (lifetimeSeconds <= 0f)
        {
            lifetimeSeconds = DefaultFallbackLifetimeSeconds;
        }

        Object.Destroy(effect, lifetimeSeconds);
    }

    private static float TryGetParticleLifetimeSeconds(GameObject instance)
    {
        // 子含めて ParticleSystem を探す
        ParticleSystem ps = instance.GetComponentInChildren<ParticleSystem>();
        if (ps == null) return -1f;

        // duration + 最大startLifetime を目安に
        float duration = ps.main.duration;

        // startLifetime は定数ではないこともあるので最大値を使う
        float lifetimeMax = 0f;
        var startLifetime = ps.main.startLifetime;

        if (startLifetime.mode == ParticleSystemCurveMode.Constant)
        {
            lifetimeMax = startLifetime.constant;
        }
        else if (startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
        {
            lifetimeMax = startLifetime.constantMax;
        }
        else
        {
            // Curve / TwoCurves はざっくり最大 1s などで扱うより、
            // いったん constantMax 相当が取れないので 0 扱いにしてフォールバックへ
            lifetimeMax = 0f;
        }

        return duration + lifetimeMax;
    }
}
