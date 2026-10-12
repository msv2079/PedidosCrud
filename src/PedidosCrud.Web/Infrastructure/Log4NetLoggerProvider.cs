using log4net;
using log4net.Config;
using log4net.Core;
using log4net.Repository;

namespace PedidosCrud.Web.Infrastructure;

public sealed class Log4NetLoggerProvider : ILoggerProvider
{
    private readonly ILoggerRepository _repository;

    public Log4NetLoggerProvider(IWebHostEnvironment environment)
    {
        _repository = LogManager.CreateRepository($"PedidosCrud-{Guid.NewGuid():N}");
        var configurationPath = Path.Combine(environment.ContentRootPath, "log4net.config");
        XmlConfigurator.Configure(_repository, new FileInfo(configurationPath));
    }

    public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
    {
        return new Log4NetLogger(LogManager.GetLogger(_repository.Name, categoryName));
    }

    public void Dispose()
    {
        _repository.Shutdown();
    }

    private sealed class Log4NetLogger(ILog logger) : Microsoft.Extensions.Logging.ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None && logger.Logger.IsEnabledFor(GetLevel(logLevel));
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                logger.Logger.Log(typeof(Log4NetLogger), GetLevel(logLevel), formatter(state, exception), exception);
            }
        }

        private static Level GetLevel(LogLevel logLevel)
        {
            return logLevel switch { LogLevel.Trace => Level.Debug, LogLevel.Debug => Level.Debug, LogLevel.Information => Level.Info, LogLevel.Warning => Level.Warn, LogLevel.Error => Level.Error, LogLevel.Critical => Level.Fatal, _ => Level.Off };
        }
    }
}
