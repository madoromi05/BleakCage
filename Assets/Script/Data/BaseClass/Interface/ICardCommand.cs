using System.Collections;

/// <summary>
/// Card効果を発動するためのコマンドインターフェース
/// </summary>
public interface ICardCommand
{
    bool Do();
    bool Undo();
}

