#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

public class AnimationEventFixer : Editor
{
    // 右クリックメニューに機能を追加
    [MenuItem("Assets/Fix Animation Events (Set OnAnimationHit)")]
    public static void FixSelectedAnimationEvents()
    {
        Object[] selectedObjects = Selection.objects;
        int fixedCount = 0;

        foreach (Object obj in selectedObjects)
        {
            AnimationClip clip = obj as AnimationClip;
            if (clip == null) continue;

            // クリップ内の全イベントを取得
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
            bool modified = false;

            for (int i = 0; i < events.Length; i++)
            {
                // 関数名が空欄、または中途半端なイベントを見つけたら修正
                if (string.IsNullOrEmpty(events[i].functionName))
                {
                    events[i].functionName = "OnAnimationHit";
                    modified = true;
                }
            }

            // 変更があった場合のみ保存
            if (modified)
            {
                AnimationUtility.SetAnimationEvents(clip, events);
                EditorUtility.SetDirty(clip);
                fixedCount++;
            }
        }

        // 変更を保存
        if (fixedCount > 0)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"完了: {fixedCount}個のアニメーションクリップを修正しました！");
        }
        else
        {
            Debug.Log("修正が必要なクリップは見つかりませんでした（またはクリップが選択されていません）。");
        }
    }
}
#endif