using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ガードゲージの状態管理と増減ロジック、UI更新を担うシステム
/// </summary>
public class GuardGaugeSystem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider guardGaugeSlider;

    public float GetCurrentGuardGauge() => currentGuardGauge;
    private float currentGuardGauge;
    private int counterCount = 0;
    private const float MAX_GUARD_GAUGE = 100f;

    public void Init()
    {
        currentGuardGauge = MAX_GUARD_GAUGE;
        UpdateGuardGaugeUI();
        counterCount = 0;
    }

    /// <summary>
    /// ガードゲージを消費できるか試行する。成功すれば消費する。
    /// </summary>
    public bool TrySpendGuardGauge(float amount)
    {
        if (currentGuardGauge >= amount)
        {
            currentGuardGauge -= amount;
            UpdateGuardGaugeUI();
            return true;
        }
        return false; // ゲージ不足
    }

    /// <summary>
    /// ガードゲージを回復させる。最大値を超えない。
    /// </summary>
    public void AddGuardGauge(float amount)
    {
        currentGuardGauge += amount;
        currentGuardGauge = Mathf.Clamp(currentGuardGauge, 0, MAX_GUARD_GAUGE);
        UpdateGuardGaugeUI();
    }

    private void UpdateGuardGaugeUI()
    {
        if (guardGaugeSlider != null)
        {
            guardGaugeSlider.value = currentGuardGauge / MAX_GUARD_GAUGE;
        }
    }

    /// <summary>
    /// カウンター回数を増やす
    /// </summary>
    public void IncrementCounterCount()
    {
        counterCount++;
    }

    /// <summary>
    /// 現在のカウンター回数を取得し、リセットする
    /// </summary>
    public int PopCounterCount()
    {
        int count = counterCount;
        counterCount = 0;
        return count;
    }
}