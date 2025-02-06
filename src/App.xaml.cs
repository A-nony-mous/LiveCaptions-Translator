using System.Windows;
using System.Windows.Automation;
using LiveCaptionsTranslator.models;

namespace LiveCaptionsTranslator
{
    public partial class App : Application
    {
        private static AutomationElement? window = null;
        private static Caption? captions = null;
        private static Setting? settings = null;

        public static AutomationElement? Window
        {
            get => window;
            set => window = value;
        }
        public static Caption? Captions
        {
            get => captions;
        }
        public static Setting? Settings
        {
            get => settings;
        }

        App()
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                Logger.LogError(args.ExceptionObject as Exception, "❌ Unhandled Exception");
            };

            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                Logger.LogError(args.Exception, "⚠️ Unobserved Task Exception");
                args.SetObserved();
            };
            
            window = LiveCaptionsHandler.LaunchLiveCaptions();
            captions = Caption.GetInstance();
            settings = Setting.Load();

            Logger.UpdateLoggingLevel(settings?.IsDebugLoggingEnabled ?? false);

            CaptureConsoleOutput();

            Task.Run(() => Captions?.Sync());
            Task.Run(() => Captions?.Translate());
        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            LiveCaptionsHandler.KillLiveCaptions();
        }
        
        private static void CaptureConsoleOutput()
        {
            Console.SetOut(new LogTextWriter(LogLevel.Information));
            Console.SetError(new LogTextWriter(LogLevel.Error));
        }

        private class LogTextWriter : System.IO.TextWriter
        {
            private readonly LogLevel _level;
            public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;

            public LogTextWriter(LogLevel level)
            {
                _level = level;
            }

            public override void WriteLine(string value)
            {
                if (_level == LogLevel.Information)
                    Logger.LogInfo(value);
                else if (_level == LogLevel.Error)
                    Logger.LogError(null, value);
            }
        }
    }
}
