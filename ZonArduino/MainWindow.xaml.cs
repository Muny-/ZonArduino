using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CoreAudioApi;
using CoreAudioApi.Interfaces;
using System.IO.Ports;
using System.Threading;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Runtime.InteropServices;

namespace ZonArduino
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        public const int KEYEVENTF_EXTENDEDKEY = 0x0001; //Key down flag
        public const int KEYEVENTF_KEYUP = 0x0002; //Key up flag
        public const int VK_MEDIA_NEXT_TRACK = 0xB0;
        public const int VK_MEDIA_PREV_TRACK = 0xB1;

        Process[] spotifies;
        Process skype;

        MMDeviceEnumerator deviceEnumerator;
        MMDevice speakers;

        DispatcherTimer peakLevelLoop;
        DispatcherTimer bpsLoop;

        float lastvol = 0;

        // false == Spotify
        // true == Skype
        bool rotaryEncoderMode = false;

        SerialPort serialPort;

        NotifyIcon notifyIcon = new NotifyIcon();

        int messagesReceived = 0;

        Thread peakLevelThread;

        public MainWindow()
        {
            InitializeComponent();

            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
            

            

            peakLevelLoop = new DispatcherTimer();
            peakLevelLoop.Interval = TimeSpan.FromMilliseconds(1);
            peakLevelLoop.Tick += peakLevelLoop_Tick;
            peakLevelLoop.Start();

            bpsLoop = new DispatcherTimer();
            bpsLoop.Interval = TimeSpan.FromMilliseconds(1000);
            bpsLoop.Tick += bpsLoop_Tick;
            bpsLoop.Start();

            notifyIcon.Icon = ZonArduino.Properties.Resources.ZonArduino;

            notifyIcon.Text = "Zön Arduino";

            notifyIcon.MouseDoubleClick += notifyIcon_MouseDoubleClick;

            notifyIcon.Visible = true;

            spotifies = System.Diagnostics.Process.GetProcessesByName("Spotify");
            if (System.Diagnostics.Process.GetProcessesByName("Skype").Count() > 0)
                skype = System.Diagnostics.Process.GetProcessesByName("Skype")[0];

            deviceEnumerator = new MMDeviceEnumerator();

            

            ConnectionsList.SelectedIndex = 0;

            //<Label x:Name="ConnectionLabel" Content="COM0 (Not Connected)" Foreground="#FFB4B4B4" Style="{StaticResource LeftPanelPlaylist}"/>
            ReloadComPorts();
            ConnectionsList.SelectionChanged += ConnectionsList_SelectionChanged;

            new Thread(delegate()
            {
                while(true)
                {
                    ReloadComPorts();

                    ConnectionsList.Dispatcher.Invoke(delegate() {
                        if (ConnectionsList.SelectedIndex != -1)
                        {
                            try
                            {
                                serialPort.Write(new byte[] { 0 }, 0, 1);

                                PortNameLabel.Dispatcher.Invoke(delegate()
                                {
                                    PortNameLabel.Content = serialPort.PortName;
                                    ConnectionStatusLabel.Content = "Connected";
                                    BaudRateLabel.Content = serialPort.BaudRate + " bits/sec";
                                    BytesToReadLabel.Content = serialPort.BytesToRead + " bytes";
                                    DataBitsLabel.Content = serialPort.DataBits + " bits";
                                    MessagesReceivedLabel.Content = messagesReceived;
                                });

                                if ((DateTime.Now - LastMessage).TotalSeconds > 2)
                                {
                                    if (ConnectionsList.SelectedIndex < ((List<System.Windows.Controls.Label>)ConnectionsList.ItemsSource).Count-1)
                                    {
                                        ConnectionsList.SelectedIndex++;
                                    }
                                    else if (ConnectionsList.Items.Count > 1)
                                    {
                                        ConnectionsList.SelectedIndex = 0;
                                    }
                                }
                            }
                            catch
                            {
                                Disconnected();
                                try
                                {
                                    serialPort.Close();
                                }
                                catch
                                {

                                }
                                serialPort.Dispose();

                                ConnectionsList.Items.MoveCurrentToNext();
                            }
                        }
                        else
                        {
                            if (ConnectionsList.Items.Count > 0)
                                ConnectionsList.SelectedIndex = 0;
                        }
                    });

                    

                    Thread.Sleep(500);
                }
            }).Start();

            VolumeStatus = new VolumeStatusWindow();
            VolumeStatus.Show();

            TrackStatus = new TrackStatusWindow();
            TrackStatus.Show();

            

            peakLevelThread = new Thread(delegate()
            {
                speakers = deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);
                float t = speakers.AudioEndpointVolume.MasterVolumeLevelScalar;

                SetMasterVolume(speakers.AudioEndpointVolume.MasterVolumeLevelScalar);
                SetSkypeVolume(0);
                SetSpotifyVolume(0);

                Thread.Sleep(1000);
                while (true)
                {
                    /*if (this.IsActive)
                    {
                        PeakLevelRectangle.Dispatcher.InvokeAsync(delegate()
                        {
                            PeakLevelRectangle.Margin = new Thickness(145, 307, speakers.AudioMeterInformation.MasterPeakValue.Map(0, 1, 187, 20), 0);
                        });
                    }*/

                    byte peak = (byte)((int)(speakers.AudioMeterInformation.MasterPeakValue.Map(0, 1, 0, 170)));
                    if (peak > 120)
                        peak = (byte)(peak * 1.5F);

                    if (serialPort != null && serialPort.IsOpen)
                        try
                        {
                            /*new Task(() =>
                            {*/
                                serialPort.Write(new byte[] { peak }, 0, 1);
                            /*}).Start();*/
                        }
                        catch
                        {

                        }

                    Thread.Sleep(1);
                }
                
            });

            peakLevelThread.Start();
            
        }

        TrackStatusWindow TrackStatus;
        VolumeStatusWindow VolumeStatus;

        int byte_counter = 0;

        static String BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (byteCount == 0)
                return "0 " + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + " " + suf[place];
        }

        void bpsLoop_Tick(object sender, EventArgs e)
        {
            Byte_ThroughputLabel.Dispatcher.Invoke(delegate()
            {
                Byte_ThroughputLabel.Content = BytesToString(byte_counter) + "/s";
            });

            byte_counter = 0;
        }

        void peakLevelLoop_Tick(object sender, EventArgs e)
        {
            
        }

        System.Windows.Controls.Label lastSelection;

        DateTime LastMessage = DateTime.Now;

        void ReloadComPorts()
        {
            ignoreChange = true;

            List<System.Windows.Controls.Label> comports = new List<System.Windows.Controls.Label>();
            int index = 0;
            int selectedIndex = -1;
            foreach (string port in SerialPort.GetPortNames())
            {
                Dispatcher.Invoke(delegate()
                {
                    System.Windows.Controls.Label label = new System.Windows.Controls.Label();
                    label.Name = port + "Label";
                    if (serialPort != null && serialPort.PortName == port)
                    {
                        label.Content = port + " (Connected)";
                        selectedIndex = index;
                    }
                    else
                        label.Content = port + " (Not Connected)";
                    label.Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180));
                    label.Style = Resources["LeftPanelPlaylist"] as Style;
                    comports.Add(label);
                });


                index++;
            }

            if (ConnectionsList.ItemsSource != null)
                ((List<System.Windows.Controls.Label>)ConnectionsList.ItemsSource).Clear();
            else
                ConnectionsList.Items.Clear();

            Dispatcher.Invoke(delegate()
            {
                try
                {
                    ConnectionsList.ItemsSource = comports;
                }
                catch
                {

                }

                ConnectionsList.SelectedIndex = selectedIndex;
            });

            ignoreChange = false;
        }

        bool ignoreChange = false;

        void ConnectionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ConnectionsList.SelectedIndex != -1 && !ignoreChange)
            {

                if (serialPort != null && serialPort.IsOpen)
                {
                    try
                    {
                        serialPort.Close();
                    }
                    catch
                    {

                    }
                    serialPort.Dispose();

                    if (lastSelection != null)
                        lastSelection.Content = lastSelection.Content.ToString().Split(' ')[0] + " (Not Connected)";
                }

                lastSelection = ((System.Windows.Controls.Label)ConnectionsList.SelectedValue);

                string comport = ((System.Windows.Controls.Label)ConnectionsList.SelectedValue).Content.ToString().Split(' ')[0];

                serialPort = new SerialPort(comport, 115200, Parity.None, 8, StopBits.One);
                ConnectionsList.Dispatcher.Invoke(delegate()
                {
                    ((System.Windows.Controls.Label)ConnectionsList.SelectedValue).Content = serialPort.PortName + " (Connecting...)";
                });

                serialPort.DataReceived += serialPort_DataReceived;
                try
                {
                    serialPort.Open();
                    messagesReceived = 0;
                    LastMessage = DateTime.Now;
                    ConnectionsList.Dispatcher.Invoke(delegate()
                    {
                        ((System.Windows.Controls.Label)ConnectionsList.SelectedValue).Content = serialPort.PortName + " (Connected)";
                    });
                }
                catch
                {
                    Disconnected();
                }
            }
        }

        void notifyIcon_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (this.Visibility == System.Windows.Visibility.Visible)
                this.Hide();
            else
            {
                this.Show();
            }
        }

        void Disconnected()
        {
            ConnectionsList.Dispatcher.Invoke(delegate()
            {
                ((System.Windows.Controls.Label)ConnectionsList.SelectedValue).Content = serialPort.PortName + " (Not Connected)";
                ConnectionStatusLabel.Content = "Not Connected";
            });
        }

        bool alternate_vfo_mode = false;

        void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            LastMessage = DateTime.Now;
            messagesReceived++;

            string line = null;

            int byt_read = serialPort.BytesToRead;

            try
            {
                line = serialPort.ReadLine();
            }
            catch
            {

            }

            byte_counter += byt_read - serialPort.BytesToRead;

            new Task(() =>
            {

                if (line != null)
                {
                    string key = "";
                    string value = "";

                    try
                    {
                        key = line.Split(':')[0];
                        value = line.Split(':')[1].Trim();
                    }
                    catch
                    {

                    }

                    //double freqMHz = double.Parse(line);

                    //Control.SetFrequency((long)(freqMHz * 1000000), false);

                    switch (key)
                    {
                        // rotary button
                        case "r":
                        {
                            // in
                            if (value == "i")
                            {

                            }
                            // out
                            else if (value == "o")
                            {
                                alternate_vfo_mode = !alternate_vfo_mode;
                            }
                        }
                        break;
                        // vfo (variable frequency oscillator)
                        case "v":
                        {
                            float pval = float.Parse(value);

                            if (alternate_vfo_mode)
                            {
                                if (pval > 0)
                                {
                                    TrackStatus.NextTrack();

                                    keybd_event(VK_MEDIA_NEXT_TRACK, 0, KEYEVENTF_EXTENDEDKEY, 0);
                                    keybd_event(VK_MEDIA_NEXT_TRACK, 0, KEYEVENTF_KEYUP, 0);
                                }
                                else if (pval < 0)
                                {
                                    TrackStatus.PreviousTrack();

                                    keybd_event(VK_MEDIA_PREV_TRACK, 0, KEYEVENTF_EXTENDEDKEY, 0);
                                    keybd_event(VK_MEDIA_PREV_TRACK, 0, KEYEVENTF_KEYUP, 0);
                                }
                            }
                            else
                            {

                                if (!rotaryEncoderMode)
                                {
                                    SetSpotifyVolume(pval / 50F);
                                }
                                else
                                {
                                    SetSkypeVolume(pval / 50F);
                                }
                            }
                        }
                        break;
                        // volume knob
                        case "l":
                        {
                            try
                            {
                                // 0-1023
                                float val = float.Parse(value);

                                if (val != lastvol)
                                {
                                    if (val <= 5)
                                        SetMasterVolume(0);
                                    else
                                    {
                                        if (lastvol - val > 15 || val - lastvol > 15)
                                        {
                                            SetMasterVolume(val / 1023F);

                                            lastvol = val;
                                        }

                                        
                                    }
                                }

                                VolumeRectangle.Dispatcher.Invoke(delegate()
                                {
                                    VolumeRectangle.Margin = new Thickness(145, 269, val.Map(0, 1023, 187, 20), 0);
                                });
                            }
                            catch
                            {

                            }

                        }
                        break;
                        // toggle radio / main switch
                        case "t":
                        {
                            // stop
                            if (value == "p")
                            {

                            }
                            // start
                            else if (value == "s")
                            {

                            }
                        }
                        break;
                        // detector type / momentary push button
                        case "d":
                        {
                            // advance / pushed
                            if (value == "a")
                            {

                            }
                        }
                        break;
                        // bandwidth mode / mode switch
                        case "b":
                        {
                            // on
                            if (value == "o")
                            {
                                rotaryEncoderMode = true;
                                SetSkypeVolume(0);
                            }
                            // off
                            else if (value == "f")
                            {
                                rotaryEncoderMode = false;
                                SetSpotifyVolume(0);
                            }
                        }
                        break;
                    }
                }
            }).Start();
        }

        public void SetSpotifyVolume(float delta)
        {
            

            // get the speakers (1st render + multimedia) device
            MMDeviceEnumerator deviceEnumerator = (MMDeviceEnumerator)(new MMDeviceEnumerator());
            MMDevice speakers = deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);

            for (int i = 0; i < speakers.AudioSessionManager.Sessions.Count; i++)
            {
                AudioSessionControl ctl = speakers.AudioSessionManager.Sessions[i];
                uint dn = ctl.ProcessID;

                if (spotifies.Where(value => value.Id == dn).Count() > 0)
                {
                    try
                    {
                        
                        ctl.SimpleAudioVolume.MasterVolume += delta;
                        
                    }
                    catch
                    {
                        try
                        {
                            if (delta > 0)
                                ctl.SimpleAudioVolume.MasterVolume += 0.005f;
                            else if (delta < 0)
                                ctl.SimpleAudioVolume.MasterVolume -= 0.005f;
                        }
                        catch
                        {

                        }
                    }

                    VolumeStatus.SetSpotifyVol(ctl.SimpleAudioVolume.MasterVolume);
                    //ctl.MasterVolume += delta;
                }

                
            }
        }

        public void SetSkypeVolume(float delta)
        {
            if (skype != null)
            {
                // get the speakers (1st render + multimedia) device
                MMDeviceEnumerator deviceEnumerator = (MMDeviceEnumerator)(new MMDeviceEnumerator());
                MMDevice speakers = deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);

                for (int i = 0; i < speakers.AudioSessionManager.Sessions.Count; i++)
                {
                    AudioSessionControl ctl = speakers.AudioSessionManager.Sessions[i];

                    if (ctl.ProcessID == skype.Id)
                    {
                        try
                        {
                            ctl.SimpleAudioVolume.MasterVolume += delta;
                            
                        }
                        catch
                        {
                            try
                            {
                                if (delta > 0)
                                    ctl.SimpleAudioVolume.MasterVolume += 0.005f;
                                else if (delta < 0)
                                    ctl.SimpleAudioVolume.MasterVolume -= 0.005f;
                            }
                            catch
                            {

                            }
                        }
                        VolumeStatus.SetSkypeVol(ctl.SimpleAudioVolume.MasterVolume);
                    }
                }
            }
        }

        public void SetMasterVolume(float volume)
        {
            

            speakers.AudioEndpointVolume.MasterVolumeLevelScalar = volume;
            VolumeStatus.SetMasterVol(volume);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            //SystemCommands.CloseWindow(this);
            notifyIcon.Visible = false;
            Environment.Exit(0);
        }

        private void MaxButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Normal)
                this.WindowState = System.Windows.WindowState.Maximized;
            else if (this.WindowState == System.Windows.WindowState.Maximized)
                this.WindowState = System.Windows.WindowState.Normal;
        }

        private void MinButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            //peakLevelLoop.Interval = TimeSpan.FromMilliseconds(0);
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            //peakLevelLoop.Interval = TimeSpan.FromMilliseconds(250);
        }
    }

    public static class ExtensionMethods
    {
        public static float Map(this float value, float fromSource, float toSource, float fromTarget, float toTarget)
        {
            return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
        }
    }
}
