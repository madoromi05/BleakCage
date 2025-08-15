using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Rendering.GPUSort;

/// <summary>
/// カードをBattle開始時の初期デッキとして保持する
/// </summary>
public class BattleCardDeck : MonoBehaviour
{
    [SerializeField] public List<CardEntity> battleCardDeck = new();            //バトル中のカードデッキ
    [SerializeField] private List<int> destructionCard = new List<int>();       // 破棄したカードのIDリスト

    public void InitFromPlayerDeck(PlayerCardDeck playerCardDeck)
    {
        if (playerCardDeck == null || playerCardDeck.DeckList == null)
        {
            Debug.LogError("Player deck is null");
            return;
        }

        StartCoroutine(WaitForDeckReady(playerCardDeck));
    }

    /// <summary>
    /// BattleDeckの生成タイミングがPlayerよりも先にならないように
    /// PlayerDeckの準備が完了するまで処理待機
    /// </summary>
    private IEnumerator WaitForDeckReady(PlayerCardDeck playerCardDeck)
    {
        while (!playerCardDeck.IsDeckReady)
        {
            yield return null;
        }

        // 準備が完了したらデッキをコピー
        ResetBattleDeck(playerCardDeck.DeckList);
    }

    /// <summary>
    /// ターン開始時に呼ぶ（デッキをリセット）
    /// </summary>
    public void ResetBattleDeck(List<CardEntity> sourceDeck)
    {
        // PlayerDeckから新しいリストを作成し、シャッフル
        battleCardDeck = new List<CardEntity>(sourceDeck);
        battleCardDeck = battleCardDeck.OrderBy(x => Random.value).ToList();

        destructionCard.Clear(); // 破棄リストをリセット
        Debug.Log("Battle deck reset. Cards: " + battleCardDeck.Count);
    }

    /// <summary>
    /// カードを引く（除外リストを考慮）
    /// </summary>
    public bool TryDrawCard(out int cardID)
    {
        // 除外リストにないカードのみ候補にする
        var candidates = battleCardDeck
            .Where(card => !destructionCard.Contains(card.ID)).ToList();

        if (candidates.Count == 0)
        {
            cardID = -1;
            return false;
        }

        // ランダムに1枚選ぶ
        int randIndex = Random.Range(0, candidates.Count);
        cardID = candidates[randIndex].ID;

        // 選んだカードを除外リストに追加（同じターンで再び引かない）
        destructionCard.Add(cardID);

        return true;
    }
}