using UnityEngine;

/// <summary>
/// キャラクターの重要なボーン（手など）への参照を保持するクラス。
/// キャラクターのプレハブのルート（Animatorと同じ場所）にアタッチしてください。
/// </summary>
public class CharacterBoneHolder : MonoBehaviour
{
    [Tooltip("武器を持たせる右手のTransform")]
    public Transform RightHandTransform;

    [Tooltip("武器を持たせる左手のTransform")]
    public Transform LeftHandTransform;
}