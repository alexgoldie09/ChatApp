/*
 * Program.cs
 * ----------
 * This is the main entry point for the Windows Forms Chat application.
 * It initializes the application settings such as DPI mode and rendering styles,
 * and then launches the main chat window via `Form1`.
 *
 * Purpose:
 * - Set up and start the Windows Forms application loop.
 * - Ensure modern rendering and scaling settings are applied.
 * - Instantiate the main form (`Form1`) which drives the entire GUI and chat logic.
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Windows_Forms_Chat
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Sets high DPI awareness for better scaling on high-resolution displays.
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            // Enables Windows theme and visual styles for controls.
            Application.EnableVisualStyles();
            // Uses the default GDI+ text rendering for compatibility.
            Application.SetCompatibleTextRenderingDefault(false);
            // Starts the application by opening the Form1 chat window.
            Application.Run(new Form1());
        }
    }
}
