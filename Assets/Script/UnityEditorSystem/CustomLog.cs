using UnityEngine;
using System.Diagnostics;

/// <summary>
/// エディタ実行時のみログを表示します。
/// </summary>
public static class DebugCostom
{
    [Conditional("UNITY_EDITOR")]
    public static void Log(string message, Object context = null)
    {
        global::UnityEngine.Debug.Log(message, context);
    }

    [Conditional("UNITY_EDITOR")]
    public static void LogError(string message, Object context = null)
    {
        global::UnityEngine.Debug.LogError(message, context);
    }

    [Conditional("UNITY_EDITOR")]
    public static void LogWarning(string message, Object context = null)
    {
        global::UnityEngine.Debug.LogWarning(message, context);
    }
}