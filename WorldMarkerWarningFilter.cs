using UnityEngine;

namespace BetterNecromancy
{
    [ModLoader.ModManager]
    public static class WorldMarkerWarningFilter
    {
        private const string ConnectingWorldMarkerWarning = "Trying send worldmarker to player in state connecting";
        private static bool _installed;

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnAssemblyLoaded, ModEntry.Namespace + ".WorldMarkerWarningFilter.OnAssemblyLoaded")]
        public static void OnAssemblyLoaded(string _)
        {
            Install();
        }

        private static void Install()
        {
            if (_installed)
                return;

            var logger = Debug.unityLogger;
            if (logger == null || logger.logHandler == null || logger.logHandler is FilteringLogHandler)
                return;

            logger.logHandler = new FilteringLogHandler(logger.logHandler);
            _installed = true;
        }

        private sealed class FilteringLogHandler : ILogHandler
        {
            private readonly ILogHandler _inner;

            public FilteringLogHandler(ILogHandler inner)
            {
                _inner = inner;
            }

            public void LogException(System.Exception exception, Object context)
            {
                _inner.LogException(exception, context);
            }

            public void LogFormat(LogType logType, Object context, string format, params object[] args)
            {
                if (ShouldSuppress(format, args))
                    return;

                _inner.LogFormat(logType, context, format, args);
            }

            private static bool ShouldSuppress(string format, object[] args)
            {
                if (!string.IsNullOrEmpty(format) &&
                    format.IndexOf(ConnectingWorldMarkerWarning, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                if (args == null || args.Length == 0)
                    return false;

                for (var i = 0; i < args.Length; i++)
                {
                    var value = args[i]?.ToString();
                    if (!string.IsNullOrEmpty(value) &&
                        value.IndexOf(ConnectingWorldMarkerWarning, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
