using System;

/// <summary>
/// 属性の定義
/// </summary>
public enum AttackAttributeType
{
    Slash,   // 斬[有利(斥力), 不利(堅牢), 普通(軟体)]
    Blunt,   // 鈍[有利(軟体), 不利(斥力), 普通(堅牢)]
    Pierce,  // 突[有利(堅牢), 不利(軟体), 普通(斥力)]
    Bullet,  // 弾[相性無]
}

public enum DefensAttributeType
{
    Hardness,   //堅牢[有利(斬), 不利(突), 普通(鈍、弾)]
    Softness,   //軟体[有利(突), 不利(鈍), 普通(斬、弾)]
    Repulsive,  //斥力[有利(鈍), 不利(斬), 普通(突、弾)]
}