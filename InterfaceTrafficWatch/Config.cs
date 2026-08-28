using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows.Forms;

namespace NetSpeed
{
    public class Config
    {
        public int timerUpdate = 500;
        public NetworkInterface nic;
        public int max = 260;
        public int maxup = 35;
        public Boolean paintAvg = true;
        public int x, y;
        public int width = 200, height = 150;

        public int font = 6;
        public int fontLegend = 5;

        Form form;

        public Config(Form form)
        {
            this.form = form;
        }


        public void ReloadConfig()
        {
            if (Application.UserAppDataRegistry.GetValue("X") != null && Application.UserAppDataRegistry.GetValue("Y") != null)
            {
                x = (int)Application.UserAppDataRegistry.GetValue("X");
                y = (int)Application.UserAppDataRegistry.GetValue("Y");
            }
            if (Application.UserAppDataRegistry.GetValue("Width") != null && Application.UserAppDataRegistry.GetValue("Height") != null)
            {
                width = (int)Application.UserAppDataRegistry.GetValue("Width");
                height = (int)Application.UserAppDataRegistry.GetValue("Height");
            }
            if (Application.UserAppDataRegistry.GetValue("Minimized") != null)
            {
                //     Boolean b = (Boolean)Application.UserAppDataRegistry.GetValue("Minimized");
                //hidden = !b;
                //Minimalizuj_Click(null,null);
            }

            if (Application.UserAppDataRegistry.GetValue("Font") != null)
            {
                font = (int)Application.UserAppDataRegistry.GetValue("Font");
            }

            if (Application.UserAppDataRegistry.GetValue("FontLegend") != null)
            {
                fontLegend = (int)Application.UserAppDataRegistry.GetValue("FontLegend");
            }

            Console.Out.WriteLine(Application.UserAppDataRegistry.GetValue("Average"));
            if (Application.UserAppDataRegistry.GetValue("Average") != null) paintAvg = System.Convert.ToBoolean(Application.UserAppDataRegistry.GetValue("Average"));
            if (Application.UserAppDataRegistry.GetValue("MaxDL") != null) max = (int)Application.UserAppDataRegistry.GetValue("MaxDL");
            if (Application.UserAppDataRegistry.GetValue("MaxUP") != null) maxup = (int)Application.UserAppDataRegistry.GetValue("MaxUP");
            if (Application.UserAppDataRegistry.GetValue("Timer") != null)
            {
                timerUpdate = (int)Application.UserAppDataRegistry.GetValue("Timer");

            }
            if (Application.UserAppDataRegistry.GetValue("Interface") != null)
            {
                String nicName = (String)Application.UserAppDataRegistry.GetValue("Interface");
                NetworkInterface[] nicArr;
                //Console.Out.WriteLine(nicName);
                nicArr = NetworkInterface.GetAllNetworkInterfaces();
                int ix = 0;
                // Add each interface name to the combo box
                for (int i = 0; i < nicArr.Length; i++)
                {
                    if (nicName != null && nicName == nicArr[i].Name)
                    {
                        nic = nicArr[i];
                        //Console.Out.WriteLine("selected:"+nic.Name);
                    }
                }
            }
        }

        public void writeConfig()
        {
            Application.UserAppDataRegistry.SetValue("X", form.Left);
            Application.UserAppDataRegistry.SetValue("Y", form.Top);
            //Application.UserAppDataRegistry.SetValue("Width", form.Width);
          //  Application.UserAppDataRegistry.SetValue("Height", form.Height);
        }



    }
}
