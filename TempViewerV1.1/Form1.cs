using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Windows.Forms.VisualStyles;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace TempViewer
{
    public partial class Form1 : Form
    {
        private Timer timer;
        private NonFocusableRichTextBox temperatureBox;
        private float? lastAlarmedTemperature = null;
        private bool errorShown = false;
        private bool isTempOK = false;
        private bool isAlarmShown = false;
        MenuStrip menu = new MenuStrip();
        private bool timeoutErrorShown = false;
        private float globalTemp = 0;
        private bool isDragging = false;
        private Point offset;

        public Form1()
        {
            SetInitialWindowPosition();
            InitializeComponent();
            InitializeWindowProperties();
            InitializeMenu();
            InitializeTemperatureBox();
            InitializeTimer();
            Menu_MouseIvent();
        }

        private void SetInitialWindowPosition()
        {
            if (Properties.Settings.Default.WindowLeft >= 0 && Properties.Settings.Default.WindowTop >= 0)
            {
                this.StartPosition = FormStartPosition.Manual;
                this.Left = Properties.Settings.Default.WindowLeft;
                this.Top = Properties.Settings.Default.WindowTop;
            }
            else
            {
                this.StartPosition = FormStartPosition.CenterScreen;
            }
        }

        private void InitializeWindowProperties()
        {
            this.StartPosition = FormStartPosition.Manual;
            this.Left = Properties.Settings.Default.WindowLeft;
            this.Top = Properties.Settings.Default.WindowTop;
            this.TopMost = Properties.Settings.Default.AlwaysOnTop;
            this.Padding = new Padding(0);
            this.Text = "Temp";
            this.Size = new Size(155, 100);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.BackColor = Color.FromArgb(64, 64, 64);
            this.MaximizeBox = false; 
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
        }

        private void InitializeMenu()
        {
            menu.Items.Clear();
            menu.ShowItemToolTips = true;
            menu.BackColor = Color.FromArgb(50, 50, 50);
            menu.Dock = DockStyle.Top;
            var alarmMenuItem = new ToolStripMenuItem
            {
                Font = new Font("Segoe UI Emoji", 10),
                ToolTipText = "Set alarm temperature",
                ForeColor = Color.WhiteSmoke,
                Image = Properties.Resources.AlarmIcon,
                Padding = new Padding(0)
            };
            alarmMenuItem.Click += SetAlarmItem_Click;
            var intervalMenuItem = new ToolStripMenuItem
            {
                Font = new Font("Segoe UI Emoji", 10),
                ToolTipText = "Set update interval",
                ForeColor = Color.WhiteSmoke,
                Image = Properties.Resources.IntervalIcon,
                Padding = new Padding(0)
            };
            intervalMenuItem.Click += SetIntervalItem_Click;
            var startupMenuItem = new ToolStripMenuItem()
            {
                ForeColor = Color.WhiteSmoke,
                Font = new Font("Segoe UI Emoji", 10),
                ToolTipText = "Application window settings",
                Padding = new Padding(0),
                Image = Properties.Resources.Startup
            };
            var minimizeOnStartupItem = new ToolStripMenuItem("Minimize on startup")
            {
                Checked = Properties.Settings.Default.MinimizeOnStartup,
                CheckOnClick = true
            };
            minimizeOnStartupItem.Click += (s, e) =>
            {
                Properties.Settings.Default.MinimizeOnStartup = true;
                Properties.Settings.Default.Save();
                InitializeMenu();
            };
            var openOnStartupItem = new ToolStripMenuItem("Open on startup")
            {
                Checked = !Properties.Settings.Default.MinimizeOnStartup,
                CheckOnClick = true
            };
            openOnStartupItem.Click += (s, e) =>
            {
                Properties.Settings.Default.MinimizeOnStartup = false;
                Properties.Settings.Default.Save();
                InitializeMenu();
            };

            startupMenuItem.DropDownItems.Add(minimizeOnStartupItem);
            startupMenuItem.DropDownItems.Add(openOnStartupItem);

            var topMostMenuItem = new ToolStripMenuItem()
            {
                ForeColor = Color.WhiteSmoke,
                Font = new Font("Segoe UI Emoji", 10),
                ToolTipText = "Window mode settings",
                Padding = new Padding(0),
                Image = Properties.Resources.TopMostIcon
            };
            var alwaysOnTopItem = new ToolStripMenuItem("Always on top")
            {
                Checked = Properties.Settings.Default.AlwaysOnTop,
                CheckOnClick = true
            };
            alwaysOnTopItem.Click += (s, e) =>
            {
                Properties.Settings.Default.AlwaysOnTop = true;
                Properties.Settings.Default.Save();
                this.TopMost = true;
                InitializeMenu();
            };
            var normalWindowItem = new ToolStripMenuItem("Normal window")
            {
                Checked = !Properties.Settings.Default.AlwaysOnTop,
                CheckOnClick = true
            };
            normalWindowItem.Click += (s, e) =>
            {
                Properties.Settings.Default.AlwaysOnTop = false;
                Properties.Settings.Default.Save();
                this.TopMost = false;
                InitializeMenu();
            };
            topMostMenuItem.DropDownItems.Add(alwaysOnTopItem);
            topMostMenuItem.DropDownItems.Add(normalWindowItem);

            var ipMenuItem = new ToolStripMenuItem
            {
                Font = new Font("Segoe UI Emoji", 10),
                ToolTipText = "Set device IP address",
                ForeColor = Color.WhiteSmoke,
                Image = Properties.Resources.IpIcon, 
                Padding = new Padding(0)
            };
            ipMenuItem.Click += SetIPItem_Click;

            menu.Items.Add(alarmMenuItem);
            menu.Items.Add(intervalMenuItem);
            menu.Items.Add(startupMenuItem);
            menu.Items.Add(topMostMenuItem);
            menu.Items.Add(ipMenuItem);

            this.MainMenuStrip = menu;
            this.Controls.Add(menu);
        }
        private void SetIPItem_Click(object sender, EventArgs e)
        {
            string currentIP = Properties.Settings.Default.DeviceIPAddress;
            string input = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter the IP address of the device:",
                "Device IP Address",
                currentIP
            );

            if (!string.IsNullOrWhiteSpace(input))
            {
                if (IPAddress.TryParse(input, out _))
                {
                    Properties.Settings.Default.DeviceIPAddress = input;
                    Properties.Settings.Default.Save();
                    MessageBox.Show($"Device IP address set to {input}", "Success");
                }
                else
                {
                    MessageBox.Show("Invalid IP address format. Please enter a valid IPv4 or IPv6 address.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private void MinimizeOnStartupItem_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.MinimizeOnStartup = true;
            Properties.Settings.Default.Save();
            InitializeMenu(); 
        }

        private void OpenOnStartupItem_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.MinimizeOnStartup = false;
            Properties.Settings.Default.Save();
            InitializeMenu();
        }

        private void SetStartupState()
        {
            if (Properties.Settings.Default.MinimizeOnStartup)
            {
                this.WindowState = FormWindowState.Minimized;
            }
            else
            {
                this.WindowState = FormWindowState.Normal;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SetStartupState(); 
        }

        private void InitializeTemperatureBox()
        {
            temperatureBox = new NonFocusableRichTextBox
            {
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                HideSelection = true,
                BackColor = this.BackColor,
                ForeColor = Color.White,
                Multiline = true,
                ScrollBars = RichTextBoxScrollBars.None,
                TabStop = false,
                Height = 30,
                Width = 170,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                Dock = DockStyle.Bottom
            };

            UpdateTemperatureText(0);
            this.Controls.Add(temperatureBox);
        }

        private void InitializeTimer()
        {
            timer = new Timer
            {
                Interval = 1000
            };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Menu_MouseIvent()
        {
            menu.MouseDown += Menu_MouseDown;
            menu.MouseMove += Menu_MouseMove;
            menu.MouseUp += Menu_MouseUp;
        }

        private void Menu_MouseDown(object sender, MouseEventArgs e)
        {
            
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                offset = e.Location;  
            }
        }

        private void Menu_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point newLocation = this.Location;
                newLocation.X += e.X - offset.X;  
                newLocation.Y += e.Y - offset.Y;  

                if (newLocation != this.Location)
                {
                    this.Location = newLocation;
                }
            }
        }

        private void Menu_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }

        private void SetAlarmItem_Click(object sender, EventArgs e)
        {

            string input = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter the alarm temperature value (°C):",
                "Alarm Temperature",
                Properties.Settings.Default.AlarmTemperature.ToString("F1")
            );

            if (string.IsNullOrWhiteSpace(input))
                return; 

            if (float.TryParse(input, out float result))
            {
                Properties.Settings.Default.AlarmTemperature = result;
                Properties.Settings.Default.Save();
                MessageBox.Show($"Alarm temperature set to {result:F1} °C", "Success");
            }
            else
            {
                MessageBox.Show("Invalid temperature format.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetIntervalItem_Click(object sender, EventArgs e)
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter the update interval (in milliseconds):",
                "Update Interval",
                Properties.Settings.Default.UpdateInterval.ToString()
            );

            if (string.IsNullOrWhiteSpace(input))
                return; 

            if (int.TryParse(input, out int interval) && (interval >= 1000) && (interval <= 10000))
            {
                Properties.Settings.Default.UpdateInterval = interval;
                Properties.Settings.Default.Save();
                timer.Interval = interval;
                MessageBox.Show($"Update interval set to {interval} ms", "Success");
            }
            else
            {
                MessageBox.Show("Please enter a valid number greater than 1000 - 10000 ms.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowErrorOnce(string message)
        {
            if (!errorShown && globalTemp == 0)
            {
                UpdateTemperatureText(0);
                errorShown = true;
                MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async Task<string> FetchTemperatureResponseAsync(string ip, int port, string request)
        {
            using (var client = new TcpClient())
            {

                var connectTask = client.ConnectAsync(ip, port);
                if (await Task.WhenAny(connectTask, Task.Delay(2000)) != connectTask)
                {
                    ShowErrorOnce("Connection timeout.");
                    UpdateTemperatureText(0);
                    return null;
                }

                using (var stream = client.GetStream())
                {
                    stream.ReadTimeout = 3000;
                    byte[] requestData = Encoding.ASCII.GetBytes(request);
                    await stream.WriteAsync(requestData, 0, requestData.Length);

                    var buffer = new byte[4096];
                    var sb = new StringBuilder();

                    while (true)
                    {
                        var readTask = stream.ReadAsync(buffer, 0, buffer.Length);
                        if (await Task.WhenAny(readTask, Task.Delay(3000)) != readTask)
                        {
                            UpdateTemperatureText(0);
                            ShowErrorOnce("Read timeout.");
                            return null;
                        }

                        int bytesRead = readTask.Result;
                        if (bytesRead == 0)
                            break;

                        sb.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
                    }

                    return sb.ToString();
                }
            }
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                await HandleTemperatureFetchAsync();
            }
            catch (Exception ex)
            {
                UpdateTemperatureText(0);
                ShowErrorOnce("Unexpected error in Timer_Tick: " + ex.Message);
            }
        }

        private async Task HandleTemperatureFetchAsync()
        {
            string ip = Properties.Settings.Default.DeviceIPAddress; ;
            int port = 80;
            string request = $"GET /temp0 HTTP/1.0\r\nHost: {ip}\r\nConnection: close\r\n\r\n";


            try
            {
                string response = await FetchTemperatureResponseAsync(ip, port, request);

                if (string.IsNullOrWhiteSpace(response))
                    return; 

               
                if (timeoutErrorShown)
                {
                    timeoutErrorShown = false; 
                }

                string[] lines = response.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length < 2)
                {
                    UpdateTemperatureText(0);
                    ShowErrorOnce("Invalid HTTP response:\n" + response);
                    return;
                }

                string temperatureString = lines[1].Trim();
                if (float.TryParse(temperatureString, out float temperature))
                {
                    int roundedTemperature = (int)Math.Round(temperature);
                    UpdateTemperatureText(roundedTemperature);

                    float alarmTemp = Properties.Settings.Default.AlarmTemperature;

                    if (roundedTemperature < alarmTemp)
                    {
                        isAlarmShown = false;
                    }
                    else if (!isAlarmShown)
                    {
                        isAlarmShown = true;
                        DialogResult result = MessageBox.Show(
                            $"ALERT! Temperature reached {roundedTemperature:F1} °C. Do you want to change the alarm temperature?",
                            "Alarm Notification",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning
                        );

                        if (result == DialogResult.Yes)
                        {
                            SetAlarmItem_Click(null, null);
                        }
                    }

                    errorShown = false;
                }
                else
                {
                    UpdateTemperatureText(0);
                    ShowErrorOnce("Format error in temperature value.");
                }
            }
            catch (TimeoutException ex)
            {
                if (!timeoutErrorShown)
                {
                    timeoutErrorShown = true;
                    UpdateTemperatureText(0);
                    ShowErrorOnce("Timeout error: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                UpdateTemperatureText(0);
                ShowErrorOnce("Unexpected error: " + ex.Message);
            }
        }



        protected override void OnFormClosing(FormClosingEventArgs e)
        {

            Properties.Settings.Default.WindowLeft = this.Left;
            Properties.Settings.Default.WindowTop = this.Top;
            Properties.Settings.Default.Save();
            base.OnFormClosing(e);
        }

        private void UpdateTemperatureText(int temperature)
        {
            if (temperatureBox == null)
                return;

            if (temperatureBox.InvokeRequired)
            {
                if (temperatureBox.IsDisposed || !temperatureBox.IsHandleCreated)
                    return;

                temperatureBox.Invoke((MethodInvoker)(() => UpdateTemperatureText(temperature)));
                return;
            }

            temperatureBox.Clear();
            temperatureBox.SelectionFont = new Font("Segoe UI", 14, FontStyle.Regular);

            string prefix = "Red Pitaya ";
            string temperatureText = (temperature == 0) ? "--°C" : $"{temperature}°C";
            globalTemp = (temperature == 0) ? 0 : temperature;

            temperatureBox.SelectionColor = Color.White;
            temperatureBox.AppendText(prefix);
            temperatureBox.SelectionColor = isAlarmShown ? Color.Yellow : Color.White;
            temperatureBox.AppendText(temperatureText);
            temperatureBox.HideSelection = true;
        }
    }
}

public class NonFocusableRichTextBox : RichTextBox
{
    protected override void OnGotFocus(EventArgs e)
    {
        Parent?.SelectNextControl(this, true, true, true, true);
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_SETFOCUS = 0x0007;
        const int WM_MOUSEACTIVATE = 0x0021;

        if (m.Msg == WM_SETFOCUS)
        {
            return;
        }

        if (m.Msg == WM_MOUSEACTIVATE)
        {
            m.Result = (IntPtr)3;
            return;
        }

        base.WndProc(ref m);
    }

    protected override bool ShowFocusCues => false;

    public NonFocusableRichTextBox()
    {
        this.ReadOnly = true;
        this.TabStop = false;
        this.Cursor = Cursors.Default; 
        this.EnableAutoDragDrop = false;
        this.SetStyle(ControlStyles.Selectable, false); 
    }
}
