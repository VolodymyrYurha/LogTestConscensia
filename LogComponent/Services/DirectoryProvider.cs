using LogComponent.Models.Enums;
using LogComponent.Services.Interfaces;

namespace LogComponent.Services
{
    public class DirectoryProvider : IDirectoryProvider
    {
        private readonly string _customAppLogPath;
        private readonly string _customTestLogPath;
        private readonly Dictionary<LogType, Func<string>> _logPaths;
        private LogType _logType;

        public DirectoryProvider(string? customAppLogPath = null, string? customTestLogPath = null, LogType logType = LogType.Application)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _customAppLogPath = customAppLogPath ?? Path.Combine(baseDir, "AppLogs");
            _customTestLogPath = customTestLogPath ?? Path.Combine(baseDir, "TestLogs");
            _logType = logType;

            _logPaths = new Dictionary<LogType, Func<string>>()
            {
                { LogType.Application, () => _customAppLogPath },
                { LogType.Test, () => _customTestLogPath }
            };
        }

        public string GetLogDirectory()
        {
            if (!_logPaths.ContainsKey(_logType))
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            }

            // Get the log path based on the LogType
            var path = _logPaths.GetValueOrDefault(_logType)?.Invoke();

            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return path;
        }
    }

}
