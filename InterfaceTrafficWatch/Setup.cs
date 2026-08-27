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
            }
            InitializeNetworkInterface();
        }

        private void button1_Click(object sender, EventArgs e)
        {

            Application.UserAppDataRegistry.SetValue("Interface", cmbInterface.Items[cmbInterface.SelectedIndex].ToString ());
            Application.UserAppDataRegistry.SetValue("MaxDL", System.Convert.ToInt32(textBox1.Text, 10));
            Application.UserAppDataRegistry.SetValue("MaxUP", System.Convert.ToInt32(textBox2.Text,10));
            Application.UserAppDataRegistry.SetValue("Timer", System.Convert.ToInt32(textBox3.Text,10));
           
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

    }
}
