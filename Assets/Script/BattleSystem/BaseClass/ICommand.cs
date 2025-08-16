using System.Collections;

/// <summary>
/// コマンドインターフェース
/// </summary>
public interface ICommand
{
    bool Do();
    bool Undo();
}

