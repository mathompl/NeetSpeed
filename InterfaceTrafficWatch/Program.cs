using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

namespace InterfaceTrafficWatch
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Set UI culture from saved user preference (Application.UserAppDataRegistry)
            try
            {
                var reg = Application.UserAppDataRegistry;
                if (reg.GetValue("UICulture") is string culture && !string.IsNullOrWhiteSpace(culture))
                {
                    try
                    {
                        var ci = new CultureInfo(culture);
                        Properties.Resources.Culture = ci;
                        CultureInfo.DefaultThreadCurrentUICulture = ci;
                        Thread.CurrentThread.CurrentUICulture = ci;
                    }
                    catch { /* ignore invalid culture and continue with system default */ }
                }
            }
            catch { }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
