using UnityEngine;

// カードのデータを管理するクラス
public class CardModel
{
    public string name;             // カード名
    public int WeaponAttack;        // AT（攻撃力）
    public Sprite icon;             // 画像（アイコン）

    // コンストラクタ（カードIDを引数にしてデータを読み込む）
    public CardModel(int cardID)
    {
        // Resourcesフォルダからカードデータを取得
        //Resources/CardEntityList/Card{cardID}.asset
        //[要変更]
        CardEntity cardEntity = Resources.Load<CardEntity>("CardEntityList/Card" + cardID);

        // 取得したデータをCardModelに反映
        WeaponAttack = cardEntity.WeaponAttack;
        icon = cardEntity.icon;
    }
}