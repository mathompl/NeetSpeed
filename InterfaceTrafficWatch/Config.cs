using System;
using System.Net.NetworkInformation;
using System.Windows.Forms;

namespace NetSpeed
{
    public class Config
    {
        public int timerUpdate = 500;
        public NetworkInterface nic;
        public string nicId;
        public string nicName;
        public int max = 260;
        public int maxup = 35;
        public Boolean paintAvg = true;
        public int x, y;
        public int width = 200, height = 150;

        public int font = 6;
        public int fontLegend = 5;

        public Boolean startMinimized = false;

        Form form;

        public Config(Form form)
        {
            this.form = form;
        }

        public static NetworkInterface FindInterface(string id, string name)
        {
            NetworkInterface[] nics;
            try
            {
                nics = NetworkInterface.GetAllNetworkInterfaces();
            }
            catch
            {
                return null;
            }

            if (nics == null || nics.Length == 0)
                return null;

            if (!string.IsNullOrEmpty(id))
            {
                foreach (NetworkInterface n in nics)
                {
                    if (n.Id == id)
                        return n;
                }
            }

            if (!string.IsNullOrEmpty(name))
            {
                foreach (NetworkInterface n in nics)
                {
                    if (string.Equals(n.Name, name, StringComparison.OrdinalIgnoreCase))
                        return n;
                }
            }

            return null;
        }

        public void ReloadConfig()
        {
            if (Application.UserAppDataRegistry.GetValue("X") != null &&
                Application.UserAppDataRegistry.GetValue("Y") != null)
            {
                x = (int)Application.UserAppDataRegistry.GetValue("X");
                y = (int)Application.UserAppDataRegistry.GetValue("Y");
            }

            if (Application.UserAppDataRegistry.GetValue("Width") != null &&
                Application.UserAppDataRegistry.GetValue("Height") != null)
            {
                width = (int)Application.UserAppDataRegistry.GetValue("Width");
                height = (int)Application.UserAppDataRegistry.GetValue("Height");
            }

            if (Application.UserAppDataRegistry.GetValue("Font") != null)
                font = (int)Application.UserAppDataRegistry.GetValue("Font");

            if (Application.UserAppDataRegistry.GetValue("FontLegend") != null)
                fontLegend = (int)Application.UserAppDataRegistry.GetValue("FontLegend");

            if (Application.UserAppDataRegistry.GetValue("Average") != null)
                paintAvg = Convert.ToBoolean(Application.UserAppDataRegistry.GetValue("Average"));

            if (Application.UserAppDataRegistry.GetValue("StartMinimized") != null)
                startMinimized = Convert.ToBoolean(Application.UserAppDataRegistry.GetValue("StartMinimized"));

            if (Application.UserAppDataRegistry.GetValue("MaxDL") != null)
                max = (int)Application.UserAppDataRegistry.GetValue("MaxDL");

            if (Application.UserAppDataRegistry.GetValue("MaxUP") != null)
                maxup = (int)Application.UserAppDataRegistry.GetValue("MaxUP");

            if (Application.UserAppDataRegistry.GetValue("Timer") != null)
                timerUpdate = (int)Application.UserAppDataRegistry.GetValue("Timer");

            nicId = Application.UserAppDataRegistry.GetValue("InterfaceId") as string;
            nicName = Application.UserAppDataRegistry.GetValue("Interface") as string;
            nic = FindInterface(nicId, nicName);

            if (nic != null)
            {
                nicId = nic.Id;
                nicName = nic.Name;
            }
        }

        public void writeConfig()
        {
            Application.UserAppDataRegistry.SetValue("X", form.Left);
            Application.UserAppDataRegistry.SetValue("Y", form.Top);
        }
    }
}