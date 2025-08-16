/*
 * Program.cs
 * ----------------------------------------------------------
 * Main entry point for the Windows Forms Chat + Tic-Tac-Toe app.
 *
 * Purpose:
 * - Start the WinForms message loop safely.
 * - Apply modern DPI / rendering (when available).
 * - Install global exception handlers so background errors don't
 *   crash the process silently.
 */

using System;
using System.Windows.Forms;

namespace Windows_Forms_Chat
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
#if NET5_0_OR_GREATER
            // .NET 5+ WinForms template helper sets DPI & styles.
            ApplicationConfiguration.Initialize();
#else
            // .NET Framework / older WinForms
            try
            {
                // Available only on .NET Core/5+; keep in try just in case.
                // If you're firmly on .NET Framework, you can remove this call.
                Application.SetHighDpiMode((HighDpiMode)1 /* SystemAware */);
            }
            catch { /* ignore if unavailable */ }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
#endif

            // Install global exception handlers so background exceptions
            // (socket callbacks, etc.) won't tear down the process silently.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) =>
            {
                try
                {
                    MessageBox.Show(
                        e.Exception.ToString(),
                        "UI Thread Exception",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch { /* last resort guard */ }
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try
                {
                    var ex = e.ExceptionObject as Exception;
                    MessageBox.Show(
                        ex?.ToString() ?? "Unknown non-UI exception",
                        "Non-UI Thread Exception",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch { /* last resort guard */ }
            };

            Application.Run(new Form1());
        }
    }
}

