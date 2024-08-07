using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZonyLrcTools.Common.Infrastructure.DependencyInject;
using ZonyLrcTools.Common.Infrastructure.Logging;

namespace ZonyLrcTools.Cli.Infrastructure.Logging
{
    public class SerilogWarpLogger : IWarpLogger, ITransientDependency
    {
        private readonly ILogger<SerilogWarpLogger> _logger;

        public SerilogWarpLogger(ILogger<SerilogWarpLogger> logger)
        {
            _logger = logger;
        }

        private Task LogAsync(Action<string, Exception> logAction, string message, Exception exception = null)
        {
            logAction(message, exception);
            return Task.CompletedTask;
        }

        public Task DebugAsync(string message, Exception exception = null)
        {
            return LogAsync(_logger.LogDebug, message, exception);
        }

        public Task InfoAsync(string message, Exception exception = null)
        {
            return LogAsync(_logger.LogInformation, message, exception);
        }

        public Task WarnAsync(string message, Exception exception = null)
        {
            return LogAsync(_logger.LogWarning, message, exception);
        }

        public Task ErrorAsync(string message, Exception exception = null)
        {
            return LogAsync(_logger.LogError, message, exception);
        }
    }
}
