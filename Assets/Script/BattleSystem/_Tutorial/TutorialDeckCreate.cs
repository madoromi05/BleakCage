using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// チュートリアル用のデッキ（1キャラのみ）を作成して返すクラス
/// </summary>
public static class TutorialDeckCreate
{
    /// <summary>
    /// PlayerDataLoaderを使って正規のデータをロードし、
    /// パーティーの1体目（と、その装備カード）だけを抽出して返す
    /// </summary>
    public static DeckSetupRepository LoadTutorialDeck()
    {
        // 既存のローダーを使って、正しいIDで構成されたフルデータを読み込む
        PlayerDataLoader loader = new PlayerDataLoader();
        DeckSetupRepository fullData = loader.LoadPlayerPartyAndCards();

        // データが空ならそのまま返す（エラー回避）
        if (fullData == null || fullData.Party == null || fullData.Party.Count == 0)
        {
            Debug.LogError("チュートリアルデッキ生成エラー: 元データが空です。");
            return fullData;
        }

        // パーティーの「1体目」だけを取り出す
        PlayerRuntime firstCharacter = fullData.Party[0];

        Debug.Log($"チュートリアル用デッキ: {firstCharacter.PlayerModel.PlayerName} (ID: {firstCharacter.ID}) のみを抽出しました。");

        // 1体だけのリストを作成して、新しいリポジトリとして返す
        List<PlayerRuntime> tutorialParty = new List<PlayerRuntime> { firstCharacter };

        return new DeckSetupRepository(tutorialParty);
    }
}