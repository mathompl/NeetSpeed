using NetSpeed;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Windows.Forms;

namespace NetSpeed
{
    public partial class Setup : Form
    {


        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunName = "NetSpeed";


        private NetworkInterface[] nicArr;

        private String nicName;
        private Config config;

        /// <summary>
        /// Initialize all network interfaces on this computer
        /// </summary>
        private void InitializeNetworkInterface()
        {
            // Grab all local interfaces to this computer
            nicArr = NetworkInterface.GetAllNetworkInterfaces();
            int ix = 0;
            // Add each interface name to the combo box
            for (int i = 0; i < nicArr.Length; i++)
            {
                if (nicName != null && nicName == nicArr[i].Name) ix = i;
                cmbInterface.Items.Add(nicArr[i].Name);
            }

            // Change the initial selection to the first interface
            cmbInterface.SelectedIndex = ix;
        }

        public Setup(Config config)
        {
            InitializeComponent();
            this.config = config;
            if (Application.UserAppDataRegistry.GetValue("MaxDL") != null)
            {
                try
                {
                    maxDlText.Text = config.max.ToString();
                    maxUpText.Text = config.maxup.ToString();
                    refreshText.Text = config.timerUpdate.ToString();
                    widthText.Text = config.width.ToString();
                    heightText.Text = config.height.ToString();
                    fontText.Text = config.font.ToString();
                    fontLegendText.Text = config.fontLegend.ToString();
                    if (config.startMinimized) startMinimizedCheckbox.Checked = true;
                    else startMinimizedCheckbox.Checked = false;
                    
                  
                    nicName = config.nic.Name.ToString();
                    if (config.paintAvg) showavgCheckbox.Checked = true;
                    else showavgCheckbox.Checked = false;

                }
                catch (Exception e)
                {
                    return;
                }
            }
            InitializeNetworkInterface();

            autostartCheckBox.Checked = IsAutoStart();
        }


        private static string ExePath()
        {
            return "\"" + Application.ExecutablePath + "\"";
        }

        public static bool IsAutoStart()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, false))
            {
                if (key == null)
                    return false;
                object v = key.GetValue(RunName);
                return v != null && string.Equals(v.ToString(), ExePath(), StringComparison.OrdinalIgnoreCase);
            }
        }

        public static void SetAutoStart(bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, true))
            {
                if (key == null)
                    return;

                if (enabled)
                    key.SetValue(RunName, ExePath());
                else
                    key.DeleteValue(RunName, false);
            }
        }



        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                nicArr = NetworkInterface.GetAllNetworkInterfaces();
                nicName = cmbInterface.SelectedItem.ToString();

                for (int i = 0; i < nicArr.Length; i++)
                {
                    if (nicName != null && nicName == nicArr[i].Name)
                    {
                        config.nic = nicArr[i];
                        break;
                    }

                }
                config.max = System.Convert.ToInt32(maxDlText.Text, 10);
                config.maxup = System.Convert.ToInt32(maxUpText.Text, 10);
                config.timerUpdate = System.Convert.ToInt32(refreshText.Text, 10);
                config.paintAvg = showavgCheckbox.Checked;
                config.startMinimized = startMinimizedCheckbox.Checked;
                config.width = System.Convert.ToInt32(widthText.Text, 10);
                config.height = System.Convert.ToInt32(heightText.Text, 10);
                config.font = System.Convert.ToInt32(fontText.Text, 10);
                config.fontLegend = System.Convert.ToInt32(fontLegendText.Text, 10); ;

                Application.UserAppDataRegistry.SetValue("Interface", config.nic.Name.ToString());
                Application.UserAppDataRegistry.SetValue("MaxDL", config.max);
                Application.UserAppDataRegistry.SetValue("MaxUP", config.maxup);
                Application.UserAppDataRegistry.SetValue("Timer", config.timerUpdate);
                Application.UserAppDataRegistry.SetValue("Average", config.paintAvg);
                Application.UserAppDataRegistry.SetValue("Width", config.width);
                Application.UserAppDataRegistry.SetValue("Height", config.height);
                Application.UserAppDataRegistry.SetValue("Font", config.font);
                Application.UserAppDataRegistry.SetValue("FontLegend", config.fontLegend);
                Application.UserAppDataRegistry.SetValue("AutoStart", autostartCheckBox.Checked);
                Application.UserAppDataRegistry.SetValue("StartMinimized", config.startMinimized);
                SetAutoStart(autostartCheckBox.Checked);

            }
            catch (Exception e3)
            {

            }
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }






        private void textBox4_Validating(object sender, CancelEventArgs e)
        {
            try
            {
                int value = System.Convert.ToInt32(widthText.Text, 10);
                if (value < 100) value = 100;
                if (value > 500) value = 500;
                widthText.Text = value + "";
            }
            catch (Exception ee)
            {
                //    textBox4.Text = 200+"";
            }
        }

        private void textBox1_Validating(object sender, CancelEventArgs e)
        {
            try
            {
                int value = System.Convert.ToInt32(maxDlText.Text, 10);
                if (value < 1) value = 1;

                maxDlText.Text = value + "";
            }
            catch (Exception ee)
            {
                maxDlText.Text = 260 + "";
            }
        }

        private void textBox2_Validating(object sender, CancelEventArgs e)
        {
            try
            {
                int value = System.Convert.ToInt32(maxUpText.Text, 10);
                if (value < 1) value = 1;

                maxUpText.Text = value + "";
            }
            catch (Exception ee)
            {
                maxUpText.Text = 35 + "";
            }
        }

        private void textBox3_Validating(object sender, CancelEventArgs e)
        {
            try
            {
                int value = System.Convert.ToInt32(refreshText.Text, 10);
                if (value < 100) value = 100;
                if (value > 10000) value = 10000;
                refreshText.Text = value + "";
            }
            catch (Exception ee)
            {
                refreshText.Text = 500 + "";
            }
        }

        private void textBox5_Validating(object sender, CancelEventArgs e)
        {
            try
            {
                int value = System.Convert.ToInt32(heightText.Text, 10);
                if (value < 100) value = 100;
                if (value > 600) value = 600;
                heightText.Text = value + "";
            }
            catch (Exception ee)
            {
                //   textBox5.Text = 150 + "";
            }
        }

  
    }
}
