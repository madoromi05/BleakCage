using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Rendering.GPUSort;

/// <summary>
/// カードをBattle開始時の初期デッキとして保持する
/// </summary>
public class BattleCardDeck : MonoBehaviour
{
    public PlayerCardDeck playerDeck;
    private List<int> drawPile; // ドロップ可能なカードIDリスト
    void Start()
    {
        drawPile = new List<int>();
    }

    public bool TryDrawCard(List<int> excludeIds, out int cardId)
    {
        var candidates = drawPile.Where(id => !excludeIds.Contains(id)).ToList();
        if (candidates.Count == 0)
        {
            cardId = -1;
            Debug.LogWarning("除外リストによりカードが引けません");
            return false;
        }

        int randIndex = Random.Range(0, candidates.Count);
        cardId = candidates[randIndex];
        return true;
    }
}