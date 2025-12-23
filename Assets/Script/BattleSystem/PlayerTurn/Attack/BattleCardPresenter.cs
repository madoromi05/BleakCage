using UnityEngine;

/// <summary>
/// 戦闘アクション実行中のカード表示を管理するクラス
/// </summary>
public class BattleCardPresenter : MonoBehaviour
{
    private CardController cardPrefab;
    private CardController currentCardInstance;
    private CardModelFactory cardModelFactory;
    private Transform targetParent;

    /// <summary>
    /// 初期化処理。PlayerTurnからFactoryを受け取る
    /// </summary>
    public void Setup(CardModelFactory factory, Transform handTransform, CardController prefab)
    {
        this.cardModelFactory = factory;
        this.targetParent = handTransform;
        this.cardPrefab = prefab;
    }

    /// <summary>
    /// カードを表示する
    /// </summary>
    public void ShowCard(CardRuntime cardRuntime)
    {
        HideCard();
        if (cardRuntime == null) return;
        currentCardInstance = Instantiate(cardPrefab, targetParent, false);
        CardModel cardModel = cardModelFactory.CreateFromID(cardRuntime.ID);
        float basePower = 0;
        if (cardRuntime.weaponRuntime != null)
        {
            var parent = cardRuntime.weaponRuntime.ParentPlayer;
            float level = parent != null ? parent.Level : 0;
            basePower = level + cardRuntime.weaponRuntime.attackPower;
        }
        currentCardInstance.Init(cardModel, basePower);

        // 攻撃中の表示用なので、操作（クリック等）はできないようにする
        currentCardInstance.SetInteractable(false);
        currentCardInstance.transform.localPosition = Vector3.zero;
    }

    /// <summary>
    /// カードを非表示にする（削除する）
    /// </summary>
    public void HideCard()
    {
        if (currentCardInstance != null)
        {
            Destroy(currentCardInstance.gameObject);
            currentCardInstance = null;
        }
    }
}