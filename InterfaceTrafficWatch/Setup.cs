using System;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using Microsoft.Win32;

namespace NetSpeed
{
    public partial class Setup : Form
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunName = "NetSpeed";

        private NetworkInterface[] nicArr;
        private string nicName;
        private string nicId;
        private Config config;

        private void InitializeNetworkInterface()
        {
            cmbInterface.Items.Clear();

            nicArr = NetworkInterface.GetAllNetworkInterfaces();
            int ix = 0;

            for (int i = 0; i < nicArr.Length; i++)
            {
                cmbInterface.Items.Add(nicArr[i].Name);

                bool matchId = !string.IsNullOrEmpty(nicId) && nicArr[i].Id == nicId;
                bool matchName = !string.IsNullOrEmpty(nicName) &&
                                 string.Equals(nicArr[i].Name, nicName, StringComparison.OrdinalIgnoreCase);

                if (matchId || matchName)
                    ix = i;
            }

            if (cmbInterface.Items.Count > 0)
                cmbInterface.SelectedIndex = ix;
        }

        public Setup(Config config)
        {
            InitializeComponent();
            this.config = config;

            try
            {
                maxDlText.Text = config.max.ToString();
                maxUpText.Text = config.maxup.ToString();
                refreshText.Text = config.timerUpdate.ToString();
                widthText.Text = config.width.ToString();
                heightText.Text = config.height.ToString();
                fontText.Text = config.font.ToString();
                fontLegendText.Text = config.fontLegend.ToString();
                startMinimizedCheckbox.Checked = config.startMinimized;
                showavgCheckbox.Checked = config.paintAvg;

                if (config.nic != null)
                {
                    nicName = config.nic.Name;
                    nicId = config.nic.Id;
                }
                else
                {
                    nicName = config.nicName;
                    nicId = config.nicId;
                }
            }
            catch
            {
                // combo i tak musi się wypełnić
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
                if (cmbInterface.SelectedItem == null)
                {
                    MessageBox.Show("Wybierz interfejs sieciowy.");
                    return;
                }

                nicName = cmbInterface.SelectedItem.ToString();
                NetworkInterface selected = Config.FindInterface(null, nicName);

                if (selected == null)
                {
                    MessageBox.Show("Nie znaleziono wybranego interfejsu.");
                    return;
                }

                config.nic = selected;
                config.nicId = selected.Id;
                config.nicName = selected.Name;

                config.max = Convert.ToInt32(maxDlText.Text, 10);
                config.maxup = Convert.ToInt32(maxUpText.Text, 10);
                config.timerUpdate = Convert.ToInt32(refreshText.Text, 10);
                config.paintAvg = showavgCheckbox.Checked;
                config.startMinimized = startMinimizedCheckbox.Checked;
                config.width = Convert.ToInt32(widthText.Text, 10);
                config.height = Convert.ToInt32(heightText.Text, 10);
                config.font = Convert.ToInt32(fontText.Text, 10);
                config.fontLegend = Convert.ToInt32(fontLegendText.Text, 10);

                Application.UserAppDataRegistry.SetValue("Interface", selected.Name);
                Application.UserAppDataRegistry.SetValue("InterfaceId", selected.Id);
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

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nie udało się zapisać ustawień:\n" + ex.Message);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void textBox4_Validating(object sender, CancelEventArgs e)
        {
            try
            {
                int value = Convert.ToInt32(widthText.Text, 10);
                if (value < 100) value = 100;
                if (value > 500) value = 500;
                widthText.Text = value.ToString();
            }
            catch
            {
            }
        }

        private void textBox1_Validating(object sender, CancelEventArgs e)
        {
            try
            {
                int value = Convert.ToInt32(maxDlText.Text, 10);
                if (value < 1) value = 1;
                maxDlText.Text = value.ToString();
            }
            catch
            {
                maxDlText.Text = "260";
            }
        }

        private void textBox2_Validating(object sender, CancelEventArgs e)
        {
            try
            {
                int value = Convert.ToInt32(maxUpText.Text, 10);
                if (value < 1) value = 1;
                maxUpText.Text = value.ToString();
            }
            catch
            {
                maxUpText.Text = "35";
            }
        }

        private void textBox3_Validating(object sender, CancelEventArgs e)
        {
            try
            {
                int value = Convert.ToInt32(refreshText.Text, 10);
                if (value < 100) value = 100;
                if (value > 10000) value = 10000;
                refreshText.Text = value.ToString();
            }
            catch
            {
                refreshText.Text = "500";
            }
        }

        private void textBox5_Validating(object sender, CancelEventArgs e)
        {
            try
            {
                int value = Convert.ToInt32(heightText.Text, 10);
                if (value < 100) value = 100;
                if (value > 600) value = 600;
                heightText.Text = value.ToString();
            }
            catch
            {
            }
        }
    }
}