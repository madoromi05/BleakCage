using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.GPUSort;

/// <summary>
/// Playerのデッキ(編集し終わった)を管理するクラス
/// </summary>
public class PlayerCardDeck : MonoBehaviour
{
    [SerializeField] private List<CardEntity>  deckList = new List<CardEntity>();             // カードのデータを保持するリスト
    public List<CardEntity> DeckList => deckList;
    private int decksheet = 42;                         // デッキの最大枚数

    public bool IsDeckReady { get; private set; } = false;

    void Start()
    {
        CreateDeck();
    }

    private void CreateDeck()
    {
        deckList.Clear();
        IsDeckReady = false; // 準備中にリセット

        for (int i = 1; i <= decksheet; i++)
        {
            CardEntity entity = Resources.Load<CardEntity>($"CardEntityList/Card_{i}");
            if (entity != null)
            {
                deckList.Add(entity);
            }
            else
            {
                Debug.LogWarning($"CardEntity not found: Card_{i}");
            }
        }
        IsDeckReady = true; // デッキ準備完了
    }
}