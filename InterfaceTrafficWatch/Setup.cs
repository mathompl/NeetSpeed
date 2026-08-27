using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using Microsoft.Win32;

namespace InterfaceTrafficWatch
{
    public partial class Setup : Form
    {

        /// <summary>
        /// Interface Storage
        /// </summary>
        private NetworkInterface[] nicArr;

        private String nicName;
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

        public Setup()
        {
            InitializeComponent();
            if (Application.UserAppDataRegistry.GetValue("MaxDL") != null)
            {
                textBox1.Text = Application.UserAppDataRegistry.GetValue("MaxDL").ToString();
                textBox2.Text = Application.UserAppDataRegistry.GetValue("MaxUP").ToString();
                textBox3.Text = Application.UserAppDataRegistry.GetValue("Timer").ToString();
                nicName = (String)Application.UserAppDataRegistry.GetValue("Interface");
                if (Application.UserAppDataRegistry.GetValue("DisplayInBits") is int dib)
                    chkDisplayInBits.Checked = dib != 0;
            }
            // Localize UI texts
            try
            {
                // handle autostart: add or remove HKCU Run entry
                try
                {
                    using (var key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                    {
                        if (key != null)
                        {
                            var exe = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                            if (chkAutoStart.Checked)
                                key.SetValue("NetSpeed", exe);
                            else
                                key.DeleteValue("NetSpeed", false);
                        }
                    }
                }
                catch { }

                // use safe accessor
                lblInterface.Text = SafeRes("Setup_Label_Interface", lblInterface.Text);
                label1.Text = SafeRes("Setup_MaxDL", label1.Text);
                label2.Text = SafeRes("Setup_MaxUP", label2.Text);
                label3.Text = SafeRes("Setup_Units_KBs", label3.Text);
                label5.Text = SafeRes("Setup_Refresh", label5.Text);
                label6.Text = SafeRes("Setup_ms", label6.Text);
                button1.Text = SafeRes("Setup_OK", button1.Text);
                button2.Text = SafeRes("Setup_Cancel", button2.Text);
                this.Text = SafeRes("Setup_Title", this.Text);
                chkDisplayInBits.Text = SafeRes("Setup_Check_DisplayInBits", chkDisplayInBits.Text);
                lblLanguage.Text = SafeRes("Setup_Label_Language", lblLanguage.Text);
            }
            catch { }

            // Populate language selection with all available cultures we added
            try
            {
                var cultureCodes = new[] {
                    "en-US","pl-PL","zh-CN","ja-JP","fr-FR","de-DE","it-IT","pt-PT","es-ES",
                    "cs-CZ","ru-RU","uk-UA","ro-RO","sk-SK","el-GR"
                };

                var items = new List<KeyValuePair<string, string>>();
                foreach (var code in cultureCodes)
                {
                    try
                    {
                        var ci = new System.Globalization.CultureInfo(code);
                        // show native name for that culture
                        items.Add(new KeyValuePair<string, string>(code, ci.NativeName));
                    }
                    catch
                    {
                        items.Add(new KeyValuePair<string, string>(code, code));
                    }
                }

                cmbLanguage.DisplayMember = "Value";
                cmbLanguage.ValueMember = "Key";
                cmbLanguage.DataSource = new System.Windows.Forms.BindingSource(items, null);

                // select saved culture if present
                try
                {
                    var saved = Application.UserAppDataRegistry.GetValue("UICulture") as string;
                    if (!string.IsNullOrEmpty(saved)) cmbLanguage.SelectedValue = saved;
                }
                catch { }

                cmbLanguage.SelectedIndexChanged += cmbLanguage_SelectedIndexChanged;
            }
            catch { }

            // Ensure interface list is populated after localization helper exists
            try { InitializeNetworkInterface(); } catch { }

            // safe resource helper for this form
            string SafeRes(string key, string def)
            {
                try { return Properties.Resources.ResourceManager.GetString(key, Properties.Resources.Culture) ?? def; } catch { return def; }
            }

            // set chkAutoStart from registry
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", false))
                {
                    if (key != null)
                    {
                        var val = key.GetValue("NetSpeed") as string;
                        if (!string.IsNullOrEmpty(val)) chkAutoStart.Checked = true;
                    }
                }
            }
            catch { }

            InitializeNetworkInterface();
        }

        private void button1_Click(object sender, EventArgs e)
        {

            Application.UserAppDataRegistry.SetValue("Interface", cmbInterface.Items[cmbInterface.SelectedIndex].ToString());
            Application.UserAppDataRegistry.SetValue("MaxDL", System.Convert.ToInt32(textBox1.Text, 10));
            Application.UserAppDataRegistry.SetValue("MaxUP", System.Convert.ToInt32(textBox2.Text,10));
            Application.UserAppDataRegistry.SetValue("Timer", System.Convert.ToInt32(textBox3.Text,10));
            Application.UserAppDataRegistry.SetValue("DisplayInBits", chkDisplayInBits.Checked ? 1 : 0);
            Application.UserAppDataRegistry.SetValue("AutoStart", chkAutoStart.Checked ? 1 : 0);
            try
            {
                if (cmbLanguage.SelectedValue is string lang)
                {
                    Application.UserAppDataRegistry.SetValue("UICulture", lang);
                    // apply culture immediately without restart
                    try
                    {
                        var ci = new System.Globalization.CultureInfo(lang);
                        Properties.Resources.Culture = ci;
                        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = ci;
                        System.Threading.Thread.CurrentThread.CurrentUICulture = ci;
                    }
                    catch { }

                    // Notify main form
                    try { MainForm.Instance?.ApplyCulture(lang); } catch { }
                }
            }
            catch { }
           
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void cmbLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (cmbLanguage.SelectedValue is string lang)
                {
                    try
                    {
                        var ci = new System.Globalization.CultureInfo(lang);
                        Properties.Resources.Culture = ci;
                        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = ci;
                        System.Threading.Thread.CurrentThread.CurrentUICulture = ci;
                    }
                    catch { }

                    // Update UI texts in this Setup form to preview
                    try
                    {
                        var rm = Properties.Resources.ResourceManager;
                        var rc = Properties.Resources.Culture;
                        lblInterface.Text = rm.GetString("Setup_Label_Interface", rc) ?? lblInterface.Text;
                        label1.Text = rm.GetString("Setup_MaxDL", rc) ?? label1.Text;
                        label2.Text = rm.GetString("Setup_MaxUP", rc) ?? label2.Text;
                        label3.Text = rm.GetString("Setup_Units_KBs", rc) ?? label3.Text;
                        label5.Text = rm.GetString("Setup_Refresh", rc) ?? label5.Text;
                        label6.Text = rm.GetString("Setup_ms", rc) ?? label6.Text;
                        button1.Text = rm.GetString("Setup_OK", rc) ?? button1.Text;
                        button2.Text = rm.GetString("Setup_Cancel", rc) ?? button2.Text;
                        this.Text = rm.GetString("Setup_Title", rc) ?? this.Text;
                        chkDisplayInBits.Text = rm.GetString("Setup_Check_DisplayInBits", rc) ?? chkDisplayInBits.Text;
                        lblLanguage.Text = rm.GetString("Setup_Label_Language", rc) ?? lblLanguage.Text;
                    }
                    catch { }
                }
            }
            catch { }
        }

    }
}
