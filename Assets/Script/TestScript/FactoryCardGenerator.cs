using UnityEngine;
using System.Collections; // IEnumerator を使うために必要

public class FactotyCardGenerator : MonoBehaviour
{
    CardModelFactory factory = new CardModelFactory();
    [SerializeField] int cardId;
    [SerializeField] CardController cardPrefab;
    [SerializeField] Transform PlayerHandTransform;

    void Start()
    {
        StartGame();
    }

    void StartGame()
    {
        CreateCard(PlayerHandTransform);
    }

   　public void CreateCard(Transform hand)
    {
        // CardControllerを生成
        CardController card = Instantiate(cardPrefab, hand, false);

        // CardModelを生成
        CardModel cardModel = factory.CreateFromId(cardId);

        // CardModelが正常に生成された場合のみ初期化
        if (cardModel != null)
        {
            card.Init(cardModel);
        }
        else
        {
            Debug.LogError($"Failed to create CardModel for ID: {cardId}");
            // カードの生成に失敗した場合は削除
            Destroy(card.gameObject);
        }
    }
}