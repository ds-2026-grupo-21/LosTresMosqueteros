using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace LosTresMosqueteros.Productos;

internal sealed class RegistroDePrueba<T> : ILogger<T>
{
    public List<Dictionary<string, object?>> Entradas { get; } = new();
    public List<Exception?> Excepciones { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Entradas.Add(((IEnumerable<KeyValuePair<string, object?>>)state!)
            .ToDictionary(x => x.Key, x => x.Value));
        Excepciones.Add(exception);
    }
}
