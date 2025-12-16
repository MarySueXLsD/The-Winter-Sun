using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace VisualNovel
{
    public partial class App : Application
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_error.log");

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Handle unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogException("DispatcherUnhandledException", e.Exception);
            e.Handled = true; // Prevent app from closing
            
            // Show error message
            MessageBox.Show(
                $"An error occurred:\n\n{e.Exception.Message}\n\nSee {LogFilePath} for details.",
                "Application Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogException("UnhandledException", ex);
            }
        }

        private static void LogException(string source, Exception ex)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logMessage = $"[{timestamp}] {source}: {ex.GetType().Name}\n";
                logMessage += $"Message: {ex.Message}\n";
                logMessage += $"Stack Trace:\n{ex.StackTrace}\n";
                if (ex.InnerException != null)
                {
                    logMessage += $"Inner Exception: {ex.InnerException.GetType().Name}\n";
                    logMessage += $"Inner Message: {ex.InnerException.Message}\n";
                }
                logMessage += new string('-', 80) + "\n\n";
                File.AppendAllText(LogFilePath, logMessage);
            }
            catch { }
        }
    }
}

