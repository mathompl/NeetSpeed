using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.NetworkInformation;

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
                var rm = NetSpeed.Properties.Resources.ResourceManager;
                var rc = NetSpeed.Properties.Resources.Culture;
                lblInterface.Text = rm.GetString("Setup_Label_Interface", rc);
                label1.Text = rm.GetString("Setup_MaxDL", rc);
                label2.Text = rm.GetString("Setup_MaxUP", rc);
                label3.Text = rm.GetString("Setup_Units_KBs", rc);
                label5.Text = rm.GetString("Setup_Refresh", rc);
                label6.Text = rm.GetString("Setup_ms", rc);
                button1.Text = rm.GetString("Setup_OK", rc);
                button2.Text = rm.GetString("Setup_Cancel", rc);
                this.Text = rm.GetString("Setup_Title", rc);
                chkDisplayInBits.Text = rm.GetString("Setup_Check_DisplayInBits", rc);
                lblLanguage.Text = rm.GetString("Setup_Label_Language", rc);
            }
            catch { }

            // Populate language selection
            try
            {
                var langs = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "pl-PL", "Polski" },
                    { "en-US", "English" },
                    { "de-DE", "Deutsch" },
                    { "es-ES", "Español" },
                    { "it-IT", "Italiano" }
                };
                cmbLanguage.DisplayMember = "Value";
                cmbLanguage.ValueMember = "Key";
                cmbLanguage.DataSource = new System.Windows.Forms.BindingSource(langs, null);

                string cur = null;
                if (Application.UserAppDataRegistry.GetValue("UICulture") is string uc)
                    cur = uc;
                if (!string.IsNullOrEmpty(cur))
                {
                    try { cmbLanguage.SelectedValue = cur; }
                    catch { }
                }
                // preview current selection when changed
                cmbLanguage.SelectedIndexChanged += cmbLanguage_SelectedIndexChanged;
            }
            catch { }

            InitializeNetworkInterface();
        }

        private void button1_Click(object sender, EventArgs e)
        {

            Application.UserAppDataRegistry.SetValue("Interface", cmbInterface.Items[cmbInterface.SelectedIndex].ToString ());
            Application.UserAppDataRegistry.SetValue("MaxDL", System.Convert.ToInt32(textBox1.Text, 10));
            Application.UserAppDataRegistry.SetValue("MaxUP", System.Convert.ToInt32(textBox2.Text,10));
            Application.UserAppDataRegistry.SetValue("Timer", System.Convert.ToInt32(textBox3.Text,10));
            Application.UserAppDataRegistry.SetValue("DisplayInBits", chkDisplayInBits.Checked ? 1 : 0);
            try
            {
                if (cmbLanguage.SelectedValue is string lang)
                {
                    Application.UserAppDataRegistry.SetValue("UICulture", lang);
                    // apply culture immediately without restart
                    try
                    {
                        var ci = new System.Globalization.CultureInfo(lang);
                        NetSpeed.Properties.Resources.Culture = ci;
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
                        NetSpeed.Properties.Resources.Culture = ci;
                        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = ci;
                        System.Threading.Thread.CurrentThread.CurrentUICulture = ci;
                    }
                    catch { }

                    // Update UI texts in this Setup form to preview
                    try
                    {
                        var rm = NetSpeed.Properties.Resources.ResourceManager;
                        var rc = NetSpeed.Properties.Resources.Culture;
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
