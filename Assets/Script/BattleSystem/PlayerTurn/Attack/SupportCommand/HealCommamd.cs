using UnityEngine;
using System.Collections;
/// <summary>
/// プレイヤーを回復するコマンド
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
        Debug.Log($"{player.PlayerModel.PlayerName} の回復実行");

        //  エフェクト再生
        yield return player.PlayerController.SupportEffect(cardModel);

        //  回復処理 (基礎値 * 出力倍率)
        float healAmount = player.Level * 10 * cardRuntime.GetOutput();
        player.HPHandler.Heal(healAmount);

        uiController.UpdateHP(player.CurrentHP);
        SoundManager.Instance.PlaySE(SEType.Heal);
    }

    public bool Undo() => false;
}