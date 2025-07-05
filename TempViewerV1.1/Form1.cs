using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Text.Json;
using System.Net;
using System.Net.Sockets;

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
        private bool isMinimizedView = false;
        private Size defaultSize;
        private Point defaultLocation;
        private Timer resizeSaveTimer = new Timer();
        private bool isFormLoaded = false;
        UdpClient udpClient = new UdpClient();

        string targetIp = Properties.Settings.Default.TargetIP;
        int targetPort = Properties.Settings.Default.TargetPort;  

        public string JsonData { get; private set; }

        public Form1()
        {
            InitializeComponent();
            SetInitialWindowPosition();
            InitializeWindowProperties();
            InitializeTemperatureBox();
            InitializeMenu();
            InitializeTimer();
            Menu_MouseIvent();

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            bool t = Properties.Settings.Default.FirstLoad;
            if (Properties.Settings.Default.FirstLoad == true)
            {
                isFormLoaded = true;
                Properties.Settings.Default.FirstLoad = false;
                t = Properties.Settings.Default.FirstLoad;
                isMinimizedView = false;
                if (!isMinimizedView)
                    EnterNormalView();
                else
                    EnterMinimizedView();
            }
            else
            {
                isMinimizedView = Properties.Settings.Default.FlagSizeForm;
                if (!isMinimizedView)
                    EnterNormalView();
                else
                    EnterMinimizedView();
            }
            
            UpdateTemperatureText(0);
        }

        private void SetInitialWindowPosition()
        {
            var savedLocation = Properties.Settings.Default.WindowLocation;
            if (savedLocation.X >= 0 && savedLocation.Y >= 0)
            {
                this.StartPosition = FormStartPosition.Manual;
                this.Location = savedLocation;
            }
            else
            {
                this.StartPosition = FormStartPosition.CenterScreen;
            }
        }

        private void InitializeWindowProperties()
        {
            this.StartPosition = FormStartPosition.Manual;
            if (Properties.Settings.Default.WindowLocation.X <= -32000 || Properties.Settings.Default.WindowLocation.Y <= -32000)
            {
                Rectangle screen = Screen.PrimaryScreen.WorkingArea;
                int formWidth = this.Width;
                int formHeight = this.Height;
                int x = (screen.Width - formWidth) / 2;
                int y = (screen.Height - formHeight) / 2;
                Properties.Settings.Default.WindowLocation = new Point(x, y);

                Properties.Settings.Default.WindowWidth = 175;
                Properties.Settings.Default.WindowHeight = 100;
            }
            this.Location = Properties.Settings.Default.WindowLocation;
            this.Width = Properties.Settings.Default.WindowWidth;
            this.Height = Properties.Settings.Default.WindowHeight;
            this.TopMost = Properties.Settings.Default.AlwaysOnTop;
            this.Padding = new Padding(0);
            this.Text = "TempViewerRP";
            this.Icon = Properties.Resources.Appicon;
            this.Size = new Size(Properties.Settings.Default.WindowWidth, Properties.Settings.Default.WindowHeight);
            this.BackColor = Properties.Settings.Default.ColorBackFomr;
            this.MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MouseDoubleClick += Form1_MouseDoubleClick;
            this.Resize += Form_Resize;
            resizeSaveTimer.Interval = 200;
            resizeSaveTimer.Tick += ResizeSaveTimer_Tick;
            this.SizeChanged += Form1_SizeChanged;
            this.Load += Form1_Load;

            if (Properties.Settings.Default.FirstLoad)
            {
                Rectangle screen = Screen.PrimaryScreen.WorkingArea;
                int formWidth = this.Width;
                int formHeight = this.Height;
                int x = (screen.Width - formWidth) / 2;
                int y = (screen.Height - formHeight) / 2;
                this.Location = new Point(x, y);
            }

            if(Properties.Settings.Default.MinimizeOnStartup)
                this.WindowState = FormWindowState.Minimized;
            //else
              //  this.Size = new Size(Properties.Settings.Default.WindowWidth, Properties.Settings.Default.WindowHeight);
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            Debug.WriteLine($"[SizeChanged] New size: {this.Size.Width}x{this.Size.Height}");

            StackTrace trace = new StackTrace(true); // true — сохраняет информацию о строках кода
            Debug.WriteLine(trace.ToString());
        }

        private void InitializeMenu()
        {
            menu.Items.Clear();
            menu.ShowItemToolTips = true;
            menu.BackColor = Properties.Settings.Default.ColorBackFomr;
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

            var customizeUIMenuItem = new ToolStripMenuItem()
            {
                Font = new Font("Segoe UI Emoji", 10),
                ToolTipText = "Customize the user interface",
                ForeColor = Color.WhiteSmoke,
                Image = Properties.Resources.SettingsIcon,
                Padding = new Padding(0),
            };
            var setColorItem = new ToolStripMenuItem("Set background color")
            {
                Font = new Font("Segoe UI Emoji", 10),
            };
            setColorItem.Click += (s, e) =>
            {
                using (ColorDialog colorDialog = new ColorDialog())
                {
                    colorDialog.Color = Properties.Settings.Default.ColorBackFomr;

                    if (colorDialog.ShowDialog() == DialogResult.OK)
                    {
                        Properties.Settings.Default.ColorBackFomr = colorDialog.Color;
                        temperatureBox.BackColor = colorDialog.Color;
                        menu.BackColor = colorDialog.Color;
                        this.BackColor = colorDialog.Color;
                        Properties.Settings.Default.Save();
                    }
                }
            };
            var setFontItem = new ToolStripMenuItem("Set font")
            {
                Font = new Font("Segoe UI Emoji", 10),
            };
            setFontItem.Click += (s, e) =>
            {
                using (FontDialog fontDialog = new FontDialog())
                using (ColorDialog colorDialog = new ColorDialog())
                {
                    // Если есть сохранённый шрифт — загружаем
                    if (Properties.Settings.Default.FontTempBox != null)
                        fontDialog.Font = Properties.Settings.Default.FontTempBox;

                    if (fontDialog.ShowDialog() == DialogResult.OK)
                    {
                        Properties.Settings.Default.FontTempBox = fontDialog.Font;
                        Properties.Settings.Default.FontTenpStyle = fontDialog.Font.Style;
                        temperatureBox.Font = Properties.Settings.Default.FontTempBox;

                        // Если есть сохранённый цвет — подставляем его в диалог цвета
                        if (Properties.Settings.Default.FontTempColor != null)
                            colorDialog.Color = Properties.Settings.Default.FontTempColor;

                        if (colorDialog.ShowDialog() == DialogResult.OK)
                        {
                            Properties.Settings.Default.FontTempColor = colorDialog.Color;
                            temperatureBox.ForeColor = Properties.Settings.Default.FontTempColor;
                        }

                        Properties.Settings.Default.Save();
                    }
                }
            };
            var udpSettingsMenuItem = new ToolStripMenuItem
            {
                Font = new Font("Segoe UI Emoji", 10),
                ToolTipText = "Enter the UDP target IP and port for Thetis Multi Meter I/O program.",
                ForeColor = Color.WhiteSmoke,
                Image = Properties.Resources.UdpIcon, 
                Padding = new Padding(0)
            };
            udpSettingsMenuItem.Click += SetUdpSettingsItem_Click;

            customizeUIMenuItem.DropDownItems.Add(setColorItem);
            customizeUIMenuItem.DropDownItems.Add(setFontItem);

            menu.Items.Add(alarmMenuItem);
            menu.Items.Add(intervalMenuItem);
            menu.Items.Add(startupMenuItem);
            menu.Items.Add(topMostMenuItem);
            menu.Items.Add(ipMenuItem);
            menu.Items.Add(customizeUIMenuItem);
            menu.Items.Add(udpSettingsMenuItem);

            this.MainMenuStrip = menu;
            this.Controls.Add(menu);
        }



        private void Form1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Point tempLoc = this.Location;
            ToggleViewMode();
            this.Location = tempLoc;
        }

        private void ToggleViewMode()
        {
            if (isMinimizedView)
                EnterNormalView();
            else
                EnterMinimizedView();
        }

        private void EnterMinimizedView()
        {
            //if (isMinimizedView)
                //return; // Уже в этом режиме

            isMinimizedView = true;

            this.FormBorderStyle = FormBorderStyle.None;
            Properties.Settings.Default.WindowLocation = this.Location;
            this.ControlBox = false;
            SetFormSizeByControlText(temperatureBox);
            //temperatureBox.Anchor = AnchorStyles.Top;
            temperatureBox.SelectAll();
            temperatureBox.SelectionAlignment = HorizontalAlignment.Left;
            temperatureBox.DeselectAll();

            foreach (Control control in this.Controls)
            {
                if (control != temperatureBox)
                    control.Visible = false;
            }
        }

        private void EnterNormalView()
        {
            isMinimizedView = false;

            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.Location = Properties.Settings.Default.WindowLocation;
            this.Width = Properties.Settings.Default.WindowWidth;
            this.Height = Properties.Settings.Default.WindowHeight;
            //temperatureBox.Anchor = AnchorStyles.Top;
            temperatureBox.Dock = DockStyle.Top;
            temperatureBox.SelectAll();
            temperatureBox.SelectionAlignment = HorizontalAlignment.Left;
            temperatureBox.DeselectAll();
            this.ControlBox = true;

            if (isFormLoaded)
            {
                Rectangle screen = Screen.PrimaryScreen.WorkingArea;
                int formWidth = this.Width;
                int formHeight = this.Height;
                int x = (screen.Width - formWidth) / 2;
                int y = (screen.Height - formHeight) / 2;
                this.Location = new Point(x, y);
                isFormLoaded = false;
            }

            foreach (Control control in this.Controls)
            {
                control.Visible = true;
            }
        }

        private void SetFormSizeByControlText(Control ctrl)
        {
            using (Graphics g = ctrl.CreateGraphics())
            {

                string tempStr = ctrl.Text;
                string temperaturePart = "--°C"; // запасной вариант

                Match match = Regex.Match(tempStr, @"(--|\d{1,3})°C");
                if (match.Success)
                {
                    temperaturePart = match.Value; // строка, например "--°C" или "25°C"
                }

                // Получаем размер текста из контрола с его шрифтом
                SizeF textSize = g.MeasureString(temperaturePart, ctrl.Font);

                int paddingWidth = 0;  // отступы по ширине
                int paddingHeight = 0; // отступы по высоте (учитываем рамки и заголовок окна)

                int newWidth = (int)Math.Ceiling(textSize.Width) + paddingWidth;
                int newHeight = (int)Math.Ceiling(textSize.Height) + paddingHeight;

                this.Size = new Size(newWidth, newHeight);
            }
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
        private void SetUdpSettingsItem_Click(object sender, EventArgs e)
        {
            using (Form udpSettingsForm = new Form())
            {
                udpSettingsForm.Text = "Set UDP Target Settings";
                udpSettingsForm.Size = new Size(300, 200);
                udpSettingsForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                udpSettingsForm.StartPosition = FormStartPosition.CenterParent;
                udpSettingsForm.MaximizeBox = false;
                udpSettingsForm.MinimizeBox = false;
                udpSettingsForm.StartPosition = FormStartPosition.CenterScreen;

                Label ipLabel = new Label() { Left = 10, Top = 20, Text = "Target IP:" };
                System.Windows.Forms.TextBox ipBox = new System.Windows.Forms.TextBox()
                {
                    Left = 120,
                    Top = 20,
                    Width = 150,
                    Text = Properties.Settings.Default.TargetIP
                };

                // Ограничиваем ввод только цифр, точек и backspace
                ipBox.KeyPress += (s, ev) =>
                {
                    char ch = ev.KeyChar;
                    if (!char.IsDigit(ch) && ch != '.' && ch != '\b')
                    {
                        ev.Handled = true;
                    }
                };

                Label portLabel = new Label() { Left = 10, Top = 60, Text = "Target Port:" };
                System.Windows.Forms.TextBox portBox = new System.Windows.Forms.TextBox()
                {
                    Left = 120,
                    Top = 60,
                    Width = 150,
                    Text = Properties.Settings.Default.TargetPort.ToString()
                };

                System.Windows.Forms.Button okButton = new System.Windows.Forms.Button()
                {
                    Text = "OK",
                    Left = 100,
                    Width = 60,
                    Top = 100,
                    DialogResult = DialogResult.OK
                };
                System.Windows.Forms.Button cancelButton = new System.Windows.Forms.Button()
                {
                    Text = "Cancel",
                    Left = 190,
                    Width = 60,
                    Top = 100,
                    DialogResult = DialogResult.Cancel
                };

                okButton.Click += (s, ev) =>
                {
                    // Проверяем корректность IP
                    if (!IPAddress.TryParse(ipBox.Text, out IPAddress ip) || ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        MessageBox.Show("Введите корректный IPv4 адрес.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    Properties.Settings.Default.TargetIP = ipBox.Text;

                    if (int.TryParse(portBox.Text, out int port))
                    {
                        Properties.Settings.Default.TargetPort = port;
                    }
                    else
                    {
                        MessageBox.Show("Invalid port number. Please enter a valid integer.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    Properties.Settings.Default.Save();
                    udpSettingsForm.Close();
                };

                udpSettingsForm.Controls.Add(ipLabel);
                udpSettingsForm.Controls.Add(ipBox);
                udpSettingsForm.Controls.Add(portLabel);
                udpSettingsForm.Controls.Add(portBox);
                udpSettingsForm.Controls.Add(okButton);
                udpSettingsForm.Controls.Add(cancelButton);

                udpSettingsForm.AcceptButton = okButton;
                udpSettingsForm.CancelButton = cancelButton;

                udpSettingsForm.ShowDialog();
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

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);          
        }

        private void Form_Resize(object sender, EventArgs e)
        {
            if (!isMinimizedView)
            {
                resizeSaveTimer.Stop();
                resizeSaveTimer.Start(); 
            }
        }

        private void ResizeSaveTimer_Tick(object sender, EventArgs e)
        {
            resizeSaveTimer.Stop();
            if(!isMinimizedView)
            {
                Properties.Settings.Default.WindowWidth = this.Width;
                Properties.Settings.Default.WindowHeight = this.Height;

                temperatureBox.Height = Properties.Settings.Default.WindowHeight;
                temperatureBox.Width = Properties.Settings.Default.WindowWidth;
                temperatureBox.SelectAll();
                temperatureBox.SelectionAlignment = HorizontalAlignment.Left;
                temperatureBox.DeselectAll();

                Properties.Settings.Default.Save();
            }
            
        }

        private void InitializeTemperatureBox()
        {
            temperatureBox = new NonFocusableRichTextBox
            {
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                HideSelection = true,
                BackColor = Properties.Settings.Default.ColorBackFomr,
                ForeColor = Properties.Settings.Default.FontTempColor,
                Multiline = true,
                ScrollBars = RichTextBoxScrollBars.None,
                TabStop = false,
                Height = Properties.Settings.Default.WindowHeight,
                Width = Properties.Settings.Default.WindowWidth,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                Dock = DockStyle.Top,
                Font = Properties.Settings.Default.FontTempBox
            };
            temperatureBox.SelectAll();
            temperatureBox.SelectionAlignment = HorizontalAlignment.Left;
            temperatureBox.DeselectAll();
            temperatureBox.MouseDoubleClick += Form1_MouseDoubleClick;
            this.Controls.Add(temperatureBox);
            UpdateTemperatureText(0);
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
                temperatureBox.MouseDown += TempBox_MouseDown;
                temperatureBox.MouseMove += TempBox_MouseMove;
                temperatureBox.MouseUp += TempBox_MouseUp;
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

        private void TempBox_MouseDown(object sender, MouseEventArgs e)
        {

            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                offset = e.Location;
            }
        }

        private void TempBox_MouseMove(object sender, MouseEventArgs e)
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

        private void TempBox_MouseUp(object sender, MouseEventArgs e)
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
                    sendDataToUdp(roundedTemperature);

                    //

                    float alarmTemp = Properties.Settings.Default.AlarmTemperature;

                    if (roundedTemperature < alarmTemp)
                    {
                        isAlarmShown = false;
                    }
                    else if (!isAlarmShown)
                    {
                        isAlarmShown = true;

                        using (Form topmostForm = new Form())
                        {
                            topmostForm.Size = new Size(0, 0);
                            topmostForm.StartPosition = FormStartPosition.Manual;
                            topmostForm.Location = new Point(-2000, -2000); // Скрываем за экраном
                            topmostForm.ShowInTaskbar = false;
                            topmostForm.TopMost = true;
                            topmostForm.Show();
                            topmostForm.Focus();

                            DialogResult result = MessageBox.Show(
                                topmostForm,
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
                            //isAlarmShown = true;
                            //DialogResult result = MessageBox.Show(
                            //    $"ALERT! Temperature reached {roundedTemperature:F1} °C. Do you want to change the alarm temperature?",
                            //    "Alarm Notification",
                            //    MessageBoxButtons.YesNo,
                            //    MessageBoxIcon.Warning
                            //);

                            //if (result == DialogResult.Yes)
                            //{
                            //    SetAlarmItem_Click(null, null);
                            //}
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
            timer.Stop();
            resizeSaveTimer.Stop();
            if (!isMinimizedView)
            {
                Properties.Settings.Default.WindowWidth = this.Width;
                Properties.Settings.Default.WindowHeight = this.Height;
            }
            if (this.WindowState != FormWindowState.Minimized && !isMinimizedView)
            {
                Properties.Settings.Default.WindowLocation = this.Location;
                Properties.Settings.Default.WindowWidth = this.Width;
                Properties.Settings.Default.WindowHeight = this.Height;
            }
            else if (this.WindowState == FormWindowState.Minimized)
            {
                Properties.Settings.Default.WindowWidth = 175;
                Properties.Settings.Default.WindowHeight = 100;
            }
            Properties.Settings.Default.FlagSizeForm = isMinimizedView;
            Properties.Settings.Default.Save();
            base.OnFormClosing(e);

        }

        private void UpdateTemperatureText(int temperature)
        {
            if (temperatureBox == null || temperatureBox.IsDisposed || !temperatureBox.IsHandleCreated)
                return;

            if (temperatureBox.InvokeRequired)
            {
                try
                {
                    temperatureBox.Invoke(new Action(() => UpdateTemperatureText(temperature)));
                }
                catch (ObjectDisposedException)
                {
                    // temperatureBox уничтожен во время Invoke — игнорируем
                }
            }
            else
            {
                try
                {
                    if (isMinimizedView)
                    {
                        temperatureBox.Clear();
                        temperatureBox.SelectionFont = new Font(Properties.Settings.Default.FontTempBox, Properties.Settings.Default.FontTenpStyle);
                        string temperatureText = (temperature == 0) ? "--°C" : $"{temperature}°C";
                        globalTemp = (temperature == 0) ? 0 : temperature;
                        temperatureBox.SelectionColor = Properties.Settings.Default.FontTempColor;
                        temperatureBox.AppendText(temperatureText);
                        temperatureBox.HideSelection = true;

                        temperatureBox.SelectAll();
                        temperatureBox.SelectionAlignment = HorizontalAlignment.Left;
                        temperatureBox.DeselectAll();
                    }
                    else
                    {
                        temperatureBox.Clear();
                        temperatureBox.SelectionFont = new Font(Properties.Settings.Default.FontTempBox, Properties.Settings.Default.FontTenpStyle);
                        string prefix = "Red Pitaya ";
                        string temperatureText = (temperature == 0) ? "--°C" : $"{temperature}°C";
                        globalTemp = (temperature == 0) ? 0 : temperature;
                        temperatureBox.SelectionColor = Properties.Settings.Default.FontTempColor;
                        temperatureBox.AppendText(prefix);
                        temperatureBox.AppendText(temperatureText);
                        temperatureBox.HideSelection = true;

                        temperatureBox.SelectAll();
                        temperatureBox.SelectionAlignment = HorizontalAlignment.Left;
                        temperatureBox.DeselectAll();
                    }
                }
                catch (ObjectDisposedException)
                {
                }
            }
            
        }
        public async void sendDataToUdp(int temperature)
        {
            var data = new { Temperature = temperature};
            JsonData = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(JsonData); // Для проверки
            try
            {
                byte[] sendBytes = System.Text.Encoding.UTF8.GetBytes(JsonData);
                await udpClient.SendAsync(sendBytes, sendBytes.Length, targetIp, targetPort);

                Console.WriteLine("Данные отправлены по UDP");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при отправке UDP: " + ex.Message);
            }
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
