using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// カードをターン開始時の初期デッキ管理クラス
/// データベースから持ってきたデータを元に、ターン開始時にデッキをリセットする
/// </summary>
public class BattleCardDeck : MonoBehaviour
{
    [SerializeField] public List<CardRuntime> battleCardDeck = new();
    [SerializeField] private List<System.Guid> destructionCardInstanceIds = new List<System.Guid>();// 破棄したカードのIDリスト

    public void InitFromCardList(List<CardRuntime> allCards)
    {
        ResetBattleDeck(allCards);
    }

    /// <summary>
    /// ターン開始時に呼ぶ（デッキをリセット）
    /// </summary>
    public void ResetBattleDeck(List<CardRuntime> sourceDeck)
    {
        battleCardDeck = new List<CardRuntime>(sourceDeck); // 新しいリストとしてコピー
        battleCardDeck = battleCardDeck.OrderBy(x => Random.value).ToList(); // シャッフル
        destructionCardInstanceIds.Clear(); // 破棄リストをリセット
        Debug.Log("Battle deck reset. Cards: " + battleCardDeck.Count);
    }

    /// <summary>
    /// カードを引く（除外リストを考慮）
    /// </summary>
    public bool TryDrawCard(out CardRuntime card)
    {
        var candidates = battleCardDeck
            .Where(c => !destructionCardInstanceIds.Contains(c.InstanceID)).ToList(); // InstanceIdで除外済みかチェック

        if (candidates.Count == 0)
        {
            card = null;
            return false;
        }

        int randIndex = Random.Range(0, candidates.Count);
        card = candidates[randIndex];

        // 破棄リストにはユニークなInstanceIdを追加する
        destructionCardInstanceIds.Add(card.InstanceID);

        return true;
    }
}