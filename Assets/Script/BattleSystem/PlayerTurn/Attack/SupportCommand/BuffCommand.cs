using UnityEngine;
using System.Collections;

public class BuffCommand : ICommand
{
    private readonly PlayerRuntime player;
    private readonly CardRuntime cardRuntime;
    private readonly PlayerStatusUIController uiController;
    private readonly CardModel cardModel;

    public BuffCommand(PlayerRuntime player, CardRuntime cardRuntime, PlayerStatusUIController ui, CardModel model)
    {
        this.player = player;
        this.cardRuntime = cardRuntime;
        this.uiController = ui;
        this.cardModel = model;
    }

    public IEnumerator Do()
    {
        DebugCostom.Log($"{player.PlayerModel.PlayerName} のバフ実行: {cardModel.StatusEffect.Type}");

        // エフェクト再生
        yield return player.PlayerController.SupportEffect(cardModel);

        // ステータス効果生成
        StatusEffect newEffect = new StatusEffect(
            cardModel.StatusEffect.Type,
            cardModel.StatusEffect.Value,
            cardModel.StatusEffect.Duration,
            cardModel.StatusEffect.InflictStacks
        );

        // Handlerへ適用
        if (player.StatusHandler != null)
        {
            player.StatusHandler.ApplyStatus(newEffect);
            uiController.UpdateStatusIcons(player.StatusHandler);
        }

        SoundManager.Instance.PlaySE(SEType.Defence);
    }

    public bool Undo() => false;
}