using System.Collections;

/// <summary>
/// コマンドインターフェース
/// </summary>
public interface ICommand
{
   IEnumerator Do();
    bool Undo();
}

