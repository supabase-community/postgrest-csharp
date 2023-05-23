using Postgrest.Exceptions;

namespace Postgrest.Interfaces;

public interface IPostgrestDebugger
{
    delegate void DebugEventHandler(object? sender, string message, PostgrestException? exception);

    void AddDebugHandler(IPostgrestDebugger.DebugEventHandler handler);
    void RemoveDebugHandler(IPostgrestDebugger.DebugEventHandler handler);
    void ClearDebugHandlers();
    void Log(object? sender, string message, PostgrestException? exception = null);
}