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
    private List<int> battleCardDeck = new();
    private List<int> destructionCard = new List<int>(); // 破棄したカードのIDリスト
    void Start()
    {
        destructionCard = new List<int>();
    }

    public void InitFromPlayerDeck(PlayerCardDeck playerCardDeck)
    {
        // プレイヤーデッキから初期デッキをコピー
        battleCardDeck = new List<int>(playerCardDeck.decklist);
        // シャッフル
        battleCardDeck = battleCardDeck.OrderBy(x => Random.value).ToList();
    }

    // 破棄したデッキを引かないようにする
    public bool TryDrawCard(List<int> excludeIds, out int cardId)
    {
        var candidates = destructionCard.Where(id => !excludeIds.Contains(id)).ToList();
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