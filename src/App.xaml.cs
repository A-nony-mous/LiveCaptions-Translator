using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;

using LiveCaptionsTranslator.models;
using LiveCaptionsTranslator.utils;

namespace LiveCaptionsTranslator
{
    public partial class App : Application, IDisposable
    {
        private static AutomationElement? window = null;
        private static Caption? captions = null;
        private static Setting? settings = null;
        private static CancellationTokenSource? cancellationTokenSource = null;

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

            window = LiveCaptionsHandler.LaunchLiveCaptions();
            LiveCaptionsHandler.FixLiveCaptions(window);
            LiveCaptionsHandler.HideLiveCaptions(window);

            captions = Caption.GetInstance();
            settings = Setting.Load();

            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            Task.Run(() => Captions?.Sync(token), token);
            Task.Run(() => Captions?.Translate(token), token);
        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            Dispose();
        }

        public static void Dispose()
        {
            if (window != null)
            {
                LiveCaptionsHandler.RestoreLiveCaptions(window);
                LiveCaptionsHandler.KillLiveCaptions(window);
            }

            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
            }

            if (captions != null)
            {
                captions.Dispose();
            }
        }
    }
}
