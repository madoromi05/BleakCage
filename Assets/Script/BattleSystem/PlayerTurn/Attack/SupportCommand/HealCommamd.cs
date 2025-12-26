using UnityEngine;
using System.Collections;

/// <summary>
/// プレイヤーを回復するコマンド
/// BuffCommandと同様の構成に修正
/// </summary>
public class HealCommand : ICommand
{
    private readonly PlayerRuntime player;
    private readonly CardRuntime cardRuntime;
    private readonly PlayerStatusUIController uiController;
    private readonly CardModel cardModel;

    public HealCommand(PlayerRuntime player, CardRuntime cardRuntime, PlayerStatusUIController ui, CardModel model)
    {
        this.player = player;
        this.cardRuntime = cardRuntime;
        this.uiController = ui;
        this.cardModel = model;
    }

    public IEnumerator Do()
    {
        yield return player.PlayerController.SupportEffect(cardModel);

        // 防御力 * カードの出力値 / 5
        float healAmount = player.PlayerModel.PlayerDefensePower * cardRuntime.GetOutput()/10;
        if (player.HPHandler != null)
        {
            player.HPHandler.Heal(healAmount);
            uiController.UpdateHP(player.CurrentHP);
        }

        // サウンド再生
        SoundManager.Instance.PlaySE(SEType.Heal);
    }

    public bool Undo() => false;
}