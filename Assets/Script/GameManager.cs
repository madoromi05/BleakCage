using UnityEngine;

public class GameManager : MonoBehaviour
{
    // カードのプレファブを入れる
    [SerializeField] CardController cardPrefab;

    // 手札のTransformを入れる
    [SerializeField] Transform PlayerHandTransform;

    void Start()
    {
        CreateCard(PlayerHandTransform);
    }

    void CreateCard(Transform hand)
    {
        // 手札にカードを生成する
        CardController card = Instantiate(cardPrefab, hand, false);

        // カードIDを指定（例: Card1）
        card.Init(1);
    }
}