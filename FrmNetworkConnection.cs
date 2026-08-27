using System;
using System.Drawing;
using System.Configuration;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Management;
using System.IO;
using Microsoft.Win32;
using System.Net.Sockets;


namespace NetworkInterfaces
{
	/// <summary>
	/// display the user the "Network and Dial-up connection" available on the computer
	/// and allow the user to choose the connection, on which the IP address change 
	/// should be performed.
	/// </summary>
	public class FrmNetworkConnection : System.Windows.Forms.Form
	{
		#region private components

		private System.Windows.Forms.Label LblSelect;
		private System.Windows.Forms.CheckedListBox ChkLbxNetworkConnections;
		private System.Windows.Forms.Button BtnNext;
		#endregion

		#region private data members
		private const int MAX_CONNECTIONS = 20;
		private int numberOfNetworkInterfaces;
		private string[] networkInterfaces = new string[MAX_CONNECTIONS];
		private string[] networkInterfacesSettingId = new string[MAX_CONNECTIONS];
		private string	selectedConnection;
		private readonly string logFileName = ConfigurationSettings.AppSettings["LogFile"];
		private readonly string processName = ConfigurationSettings.AppSettings["processName"];
		private RegistryKey currentMachineRegistry = Registry.LocalMachine;
		#endregion
		private System.Windows.Forms.Button BtnClose;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		#region public setters and getters
		public string connectionName
		{
			get
			{
				return selectedConnection;//connection;
			}
		}

		public int numOfNetworkInterfaces
		{
			get
			{
				return numberOfNetworkInterfaces;
			}
		}
		#endregion

		public string GetSelectedNetworkInterfaceName()
		{
			if(ChkLbxNetworkConnections.SelectedIndex >= 0)
				return ChkLbxNetworkConnections.SelectedItem.ToString();
			else
				return "none";
		}

		public string GetSelectedNetworkInterfaceSettingId()
		{
			int i=-1;
			if(ChkLbxNetworkConnections.SelectedIndex >= 0)
			{
				i = Array.BinarySearch(networkInterfaces, 0, numberOfNetworkInterfaces, ChkLbxNetworkConnections.SelectedItem.ToString());
				if(i >= 0)
					return networkInterfacesSettingId[i];
				else
					return "none";
			}
			else
				return "none";
		}

		public FrmNetworkConnection()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			ChkLbxNetworkConnections.CheckOnClick = true;
			this.BtnNext.Enabled = false;
			
			// extract all the ip enabled adapters and get their SettingID (which is the registry key)
			ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration"); 
			ManagementObjectCollection objMOC = objMC.GetInstances(); 
			numberOfNetworkInterfaces = 0;

			foreach(ManagementObject objMO in objMOC) 
			{ 
				if( Convert.ToBoolean(objMO["ipEnabled"]) == false )
					continue; 

				string SettingID = "";
				try
				{
					networkInterfacesSettingId[numberOfNetworkInterfaces] = (string) objMO["SettingID"];
					++numberOfNetworkInterfaces;
				}
				catch{}
			}

			if(numberOfNetworkInterfaces == 0)
			{
				MessageBox.Show("No Network Interface were found on this local machine", 
					"Error", 
					MessageBoxButtons.OK, 
					MessageBoxIcon.Error);

				this.BtnNext.Enabled = false;
			}

			int j=0;
			// The network interfaces are in the following path in the registry:
			// HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\control\Network\{xxxxxx}\SettingID\connection
			RegistryKey networkRegistry = currentMachineRegistry.OpenSubKey("SYSTEM\\CurrentControlSet\\control\\Network");
			int num = networkRegistry.SubKeyCount;
			string[] tmp = networkRegistry.GetSubKeyNames();
			for(int i=0; i<num; ++i)
			{
				// get all the {xxxxxx} keys under 'Network'
				RegistryKey conReg = networkRegistry.OpenSubKey(tmp[i]);
				if(conReg != null)
				{
					string[] tmp1 = conReg.GetSubKeyNames();
					if(tmp1.Length > 0)
					{
						for(int k=0; k<tmp1.Length; ++k)
						{
							// get all the <SettingID> keys under 'Network\{xxxxxx}'
							RegistryKey reg = conReg.OpenSubKey(tmp1[k]);
							if(reg != null)
							{
								int gg = Array.BinarySearch(networkInterfacesSettingId, 0, numberOfNetworkInterfaces, tmp1[k]);
								if(gg >= 0)
								{
									// This subkey was found in the networkInterfacesSettingId array - which means
									// that this is a valid network interface - get the interface name											
									// get the 'connection' key under 'Network\{xxxxxx}\<SettingID>'
									RegistryKey r = reg.OpenSubKey("connection");
									if(r != null)
									{
										Object obj = r.GetValue("Name");
										networkInterfaces[gg] = obj.ToString();
									}
								}
							}
						}
					}
				}
			}

			string strDebugConnectionsList = "These are the " + numberOfNetworkInterfaces + " networkInterfaces that we've found:\n";
			for(int g = 0; g<numberOfNetworkInterfaces; ++g)
			{
				strDebugConnectionsList = strDebugConnectionsList + "   " + networkInterfaces[g] + "\n";
				ChkLbxNetworkConnections.Items.Add(networkInterfaces[g], false);
			}
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			
			FrmNetworkConnection  networkConnectionFrm = new FrmNetworkConnection();
			Application.Run(networkConnectionFrm);
			
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.LblSelect = new System.Windows.Forms.Label();
			this.ChkLbxNetworkConnections = new System.Windows.Forms.CheckedListBox();
			this.BtnNext = new System.Windows.Forms.Button();
			this.BtnClose = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// LblSelect
			// 
			this.LblSelect.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(177)));
			this.LblSelect.Location = new System.Drawing.Point(24, 48);
			this.LblSelect.Name = "LblSelect";
			this.LblSelect.Size = new System.Drawing.Size(272, 40);
			this.LblSelect.TabIndex = 1;
			this.LblSelect.Text = "Please select the Network connection, to which you wish to configure the IP prope" +
				"rties";
			// 
			// ChkLbxNetworkConnections
			// 
			this.ChkLbxNetworkConnections.Location = new System.Drawing.Point(24, 104);
			this.ChkLbxNetworkConnections.Name = "ChkLbxNetworkConnections";
			this.ChkLbxNetworkConnections.Size = new System.Drawing.Size(272, 94);
			this.ChkLbxNetworkConnections.TabIndex = 2;
			this.ChkLbxNetworkConnections.ThreeDCheckBoxes = true;
			this.ChkLbxNetworkConnections.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.ChkLbxNetworkConnections_ItemCheck);
			// 
			// BtnNext
			// 
			this.BtnNext.Location = new System.Drawing.Point(136, 240);
			this.BtnNext.Name = "BtnNext";
			this.BtnNext.TabIndex = 3;
			this.BtnNext.Text = "Next >>";
			this.BtnNext.Click += new System.EventHandler(this.BtnNext_Click);
			// 
			// BtnClose
			// 
			this.BtnClose.Location = new System.Drawing.Point(224, 240);
			this.BtnClose.Name = "BtnClose";
			this.BtnClose.TabIndex = 4;
			this.BtnClose.Text = "Close";
			this.BtnClose.Click += new System.EventHandler(this.BtnCancel_Click);
			// 
			// FrmNetworkConnection
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(320, 273);
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.BtnNext,
																		  this.BtnClose,
																		  this.ChkLbxNetworkConnections,
																		  this.LblSelect});
			this.HelpButton = true;
			this.Name = "FrmNetworkConnection";
			this.Text = "Network Interface Selection";
			this.ResumeLayout(false);

		}
		#endregion

		private void BtnNext_Click(object sender, System.EventArgs e)
		{
			object si = ChkLbxNetworkConnections.SelectedItem;
			if(si == null)
			{
				MessageBox.Show("You need to select a network connection", 
					"Error", 
					MessageBoxButtons.OK, 
					MessageBoxIcon.Error);
			}

			else
			{
				selectedConnection = si.ToString();
				string s = "User selection: " + selectedConnection;
				MessageBox.Show(s, "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			DialogResult r = MessageBox.Show("Are you sure you want to quit?", 
				"Application", 
				MessageBoxButtons.YesNoCancel, 
				MessageBoxIcon.Question);
			if(r != DialogResult.Yes)
			{
				e.Cancel = true;
			}
			base.OnClosing(e);
		}

		private void BtnCancel_Click(object sender, System.EventArgs e)
		{
			this.Close();
		}

		private void ChkLbxNetworkConnections_ItemCheck(object sender, System.Windows.Forms.ItemCheckEventArgs e)
		{
			// enable the next button
			object si = ChkLbxNetworkConnections.SelectedItem;
			if(si != null && e.NewValue == CheckState.Checked)
			{
				BtnNext.Enabled = true;
			}
			else
			{
				BtnNext.Enabled = false;
			}

			// This item is going to be toggled to the checked state if this is true.
			if (e.NewValue == CheckState.Checked)
			{
				ChkLbxNetworkConnections.BeginUpdate();
				// Uncheck all checked items.
				foreach (int index in ChkLbxNetworkConnections.CheckedIndices)
				{
					ChkLbxNetworkConnections.SetItemChecked(index, false);
				} 
				ChkLbxNetworkConnections.EndUpdate();
			}
		}
	}
}
