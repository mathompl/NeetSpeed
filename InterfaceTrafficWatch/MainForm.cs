using NetSpeed;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Windows.Forms;

namespace NetSpeed
{
    public partial class MainForm : Form
    {
        private Timer timer;
        private readonly List<int> dataIn = new List<int>();
        private readonly List<int> dataOut = new List<int>();

        private long bytesSentLast;
        private long bytesReceivedLast;
        private bool hasBaseline;
        private string nicId;

        private Drawing drawing;
        private Config config;
        private bool topMost;
        private Point pos;
        private bool hidden;

        private int maxSamples = 120;
        private const int MaxSpeedKBs = 10 * 1024 * 1024; // 10 GB/s — odcinamy śmieci po wrapie

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect,
    int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);


        public MainForm()
        {
            config = new Config(this);
            ReloadConfig();
            InitializeComponent();

            if (config.nic == null)
                InitializeNetwork();

            initWindows();
            InitializeTimer();
            drawing = new Drawing(this, config, notifyIcon1);


            if (config.startMinimized)
                BeginInvoke(new Action(Minimize));
        }

        private void initWindows()
        {
            toolStripMenuItem5.Text = "Hide to tray";
            this.FormBorderStyle = FormBorderStyle.None;
            this.TransparencyKey = Color.FromArgb(255, 128, 128);
            this.BackgroundImageLayout = ImageLayout.None;
            this.Left = config.x;
            this.Top = config.y;
            this.Width = Math.Max(16, config.width);
            this.Height = Math.Max(16, config.height);
            maxSamples = this.Width - 20;
            ApplyRoundedCorners(12);

        }


        private void ApplyRoundedCorners(int radius)
        {
            if (radius < 1)
                radius = 1;

            IntPtr rgn = CreateRoundRectRgn(0, 0, Width + 1, Height + 1, radius, radius);
            SetWindowRgn(Handle, rgn, true);
            DeleteObject(rgn);
        }

        public void ReloadConfig()
        {
            try
            {
                config.ReloadConfig();

                if (timer != null)
                {
                    timer.Stop();
                    timer.Tick -= timer_Tick;
                    timer.Dispose();
                    timer = null;
                    InitializeTimer();
                }
            }
            catch (Exception ex)
            {
                DebugError("ReloadConfig", ex);
            }
        }

        private void InitializeNetwork()
        {
            try
            {
                NetworkInterface nic = FindNic();
                config.nic = nic;
                nicId = nic != null ? nic.Id : null;
                ResetCounters();
            }
            catch (Exception ex)
            {
                config.nic = null;
                nicId = null;
                DebugError("InitializeNetwork", ex);
            }
        }

        private NetworkInterface FindNic()
        {
            NetworkInterface[] nics;
            try
            {
                nics = NetworkInterface.GetAllNetworkInterfaces();
            }
            catch (Exception ex)
            {
                DebugError("GetAllNetworkInterfaces", ex);
                return null;
            }

            if (nics == null || nics.Length == 0)
                return null;

            string wantedId = nicId;
            if (string.IsNullOrEmpty(wantedId) && config.nic != null)
                wantedId = config.nic.Id;

            if (!string.IsNullOrEmpty(wantedId))
            {
                foreach (NetworkInterface n in nics)
                {
                    if (n.Id == wantedId)
                        return n;
                }
            }

            foreach (NetworkInterface n in nics)
            {
                if (n.OperationalStatus == OperationalStatus.Up &&
                    n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                {
                    return n;
                }
            }

            return nics[0];
        }

        private static bool TryGetTotals(NetworkInterface nic, out long sent, out long received)
        {
            sent = 0;
            received = 0;
            if (nic == null)
                return false;

            try
            {
                IPInterfaceStatistics ip = nic.GetIPStatistics();
                if (ip != null)
                {
                    sent = ip.BytesSent;
                    received = ip.BytesReceived;
                    return true;
                }
            }
            catch (NetworkInformationException)
            {
            }
            catch (Exception ex)
            {
                DebugError("GetIPStatistics", ex);
            }

            try
            {
                IPv4InterfaceStatistics v4 = nic.GetIPv4Statistics();
                if (v4 != null)
                {
                    sent = v4.BytesSent;
                    received = v4.BytesReceived;
                    return true;
                }
            }
            catch (NetworkInformationException)
            {
            }
            catch (Exception ex)
            {
                DebugError("GetIPv4Statistics", ex);
            }

            return false;
        }

        private void ResetCounters()
        {
            bytesSentLast = 0;
            bytesReceivedLast = 0;
            hasBaseline = false;
        }
        private void InitializeTimer()
        {
            int interval = 1000;
            try
            {
                interval = (int)config.timerUpdate;
            }
            catch
            {
                interval = 1000;
            }

            if (interval < 100)
                interval = 100;
            if (interval > 60000)
                interval = 60000;

            timer = new Timer();
            timer.Interval = interval;
            timer.Tick += timer_Tick;
            timer.Start();
        }

        private void UpdateNetworkInterface()
        {
            try
            {
                if (config == null)
                    return;

                NetworkInterface nic = FindNic();
                if (nic == null)
                {
                    InitializeNetwork();
                    return;
                }

                if (nicId != nic.Id)
                {
                    nicId = nic.Id;
                    config.nic = nic;
                    ResetCounters();
                }
                else
                {
                    config.nic = nic;
                }

                long bytesSent;
                long bytesReceived;
                if (!TryGetTotals(nic, out bytesSent, out bytesReceived))
                {
                    ResetCounters();
                    return;
                }

                if (!hasBaseline)
                {
                    bytesSentLast = bytesSent;
                    bytesReceivedLast = bytesReceived;
                    hasBaseline = true;
                    return;
                }

                long sentDelta = CounterDelta(bytesSentLast, bytesSent);
                long recvDelta = CounterDelta(bytesReceivedLast, bytesReceived);

                bytesSentLast = bytesSent;
                bytesReceivedLast = bytesReceived;

                int deltaMs = timer != null ? timer.Interval : 1000;
                if (deltaMs <= 0)
                    deltaMs = 1000;

                double bytesSentSpeed = sentDelta / 1024.0 * (1000.0 / deltaMs);
                double bytesReceivedSpeed = recvDelta / 1024.0 * (1000.0 / deltaMs);

                bytesSentSpeed = ClampSpeed(bytesSentSpeed);
                bytesReceivedSpeed = ClampSpeed(bytesReceivedSpeed);

                AddSample(dataIn, (int)Math.Round(bytesReceivedSpeed));
                AddSample(dataOut, (int)Math.Round(bytesSentSpeed));

                //  if (drawing != null)
                {
                    drawing.paintAll(
                        bytesReceivedSpeed,
                        GetAvg(dataIn),
                        bytesSentSpeed,
                        GetAvg(dataOut),
                        dataIn,
                        dataOut);
                }

                Refresh();
            }
            catch (Exception ex)
            {
                DebugError("UpdateNetworkInterface", ex);
            }
        }

        /// <summary>
        /// Różnica licznika z obsługą zawinięcia (32-bit i 64-bit).
        /// Przy podejrzanym skoku resetujemy bazę zamiast pokazywać kosmos.
        /// </summary>
        private static long CounterDelta(long last, long current)
        {
            if (current >= last)
                return current - last;

            const long Max32 = uint.MaxValue;
            if (last <= Max32 && current >= 0)
            {
                long wrapped = (Max32 - last) + current + 1;
                if (wrapped >= 0 && wrapped <= Max32)
                    return wrapped;
            }

            return 0;
        }

        private static double ClampSpeed(double kbPerSec)
        {
            if (double.IsNaN(kbPerSec) || double.IsInfinity(kbPerSec) || kbPerSec < 0)
                return 0;
            if (kbPerSec > MaxSpeedKBs)
                return 0;
            return kbPerSec;
        }

        private void AddSample(List<int> data, int value)
        {
            if (value < 0)
                value = 0;

            data.Add(value);
            while (data.Count > maxSamples)
                data.RemoveAt(0);
        }

        private int GetAvg(IList<int> data)
        {
            if (data == null || data.Count == 0)
                return 0;

            long sum = 0;
            for (int i = 0; i < data.Count; i++)
                sum += data[i];

            long avg = sum / data.Count;
            if (avg > int.MaxValue)
                return int.MaxValue;
            if (avg < 0)
                return 0;
            return (int)avg;
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            UpdateNetworkInterface();
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
                Hide();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Minimize();
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            try
            {
                if (timer != null)
                {
                    timer.Stop();
                    timer.Tick -= timer_Tick;
                    timer.Dispose();
                    timer = null;
                }
                config.writeConfig();
            }
            catch (Exception ex)
            {
                DebugError("Exit", ex);
            }
            Application.Exit();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            try
            {
                Setup setup = new Setup(config);
                config.writeConfig();
                setup.ShowDialog();
                ReloadConfig();
                nicId = config.nicId ?? (config.nic != null ? config.nic.Id : null);
                InitializeNetwork();
                //dataIn.Clear();
                //dataOut.Clear();
                initWindows();
            }
            catch (Exception ex)
            {
                DebugError("Setup", ex);
            }
        }

        private void SetTopMost()
        {
            if (topMost)
            {
                TopMost = false;
                toolStripMenuItem6.Font = new Font(toolStripMenuItem1.Font, FontStyle.Regular);
                toolStripMenuItem6.Checked = false;
            }
            else
            {
                if (hidden)
                    Minimize();
                TopMost = true;
                toolStripMenuItem6.Font = new Font(toolStripMenuItem1.Font, FontStyle.Bold);
                toolStripMenuItem6.Checked = true;
                this.Focus();
            }
            topMost = TopMost;
        }

        private void toolStripMenuItem1_MouseDown(object sender, MouseEventArgs e)
        {
            pos = e.Location;
        }

        private void toolStripMenuItem1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Left += (e.X - pos.X);
                this.Top += (e.Y - pos.Y);
            }
        }

        private void Minimize()
        {
            if (topMost)
                SetTopMost();

            if (hidden)
            {
                Show();
                hidden = false;
                toolStripMenuItem5.Text = "Hide to tray";
                this.Focus();
            }
            else
            {
                Hide();
                hidden = true;
                toolStripMenuItem5.Text = "Maximize";
            }
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            try
            {
                using (AboutBox1 box = new AboutBox1())
                    box.ShowDialog();
            }
            catch (Exception ex)
            {
                DebugError("About", ex);
            }
        }

        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            this.Focus();
        }

        private void toolStripMenuItem9_Click(object sender, EventArgs e)
        {
            toolStripMenuItem4_Click(sender, e);
        }

        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            SetTopMost();
        }

        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            Minimize();
        }

        private void toolStripMenuItem7_Click(object sender, EventArgs e)
        {
            toolStripMenuItem2_Click(sender, e);
        }

        private void toolStripMenuItem8_Click(object sender, EventArgs e)
        {
            toolStripMenuItem3_Click(sender, e);
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            Minimize();
        }

        private static void DebugError(string where, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(where + ": " + ex);
        }
    }
}