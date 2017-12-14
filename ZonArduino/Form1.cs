using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoreAudioApi;
using CoreAudioApi.Interfaces;

namespace ZonArduino
{
    public partial class Form1 : Form
    {
        Process[] spotifies;
        Process skype;

        MMDeviceEnumerator deviceEnumerator;
        MMDevice speakers;


        public Form1()
        {
            InitializeComponent();

            spotifies = System.Diagnostics.Process.GetProcessesByName("Spotify");
            skype = System.Diagnostics.Process.GetProcessesByName("Skype")[0];

            deviceEnumerator = new MMDeviceEnumerator();

            speakers = deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);
            float t = speakers.AudioEndpointVolume.MasterVolumeLevelScalar;

            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "Connect")
            {
                serialPort1.Open();

                

                button1.Text = "Disconnect";
            }
            else
            {
                button1.Text = "Connect";
                try
                {
                    serialPort1.Close();
                }
                catch
                {

                }
            }
        }

        float lastvol = 0;

        // false == Spotify
        // true == Skype
        bool rotaryEncoderMode = false;

        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
           
            string line = serialPort1.ReadLine();

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
                    case "rotarybutton":
                    {
                        if (value == "in")
                        {
                            
                        }
                        else if (value == "out")
                        {
                            
                        }
                    }
                    break;

                    case "vfo":
                    {
                        float pval = float.Parse(value);

                        if (!rotaryEncoderMode)
                        {
                            SetSpotifyVolume(pval / 50F);
                        }
                        else
                        {
                            SetSkypeVolume(pval / 50F);
                        }
                    }
                    break;

                    case "vol":
                    {
                        // 0-1023
                            float val = float.Parse(value);

                            if (val != lastvol)
                            {
                                SetMasterVolume(val / 1023F);
                                lastvol = val;
                            }
                        
                    }
                    break;

                    case "toggleradio":
                    {
                        if (value == "stop")
                        {
                            
                        }
                        else if (value == "start")
                        {
                            
                        }
                    }
                    break;

                    case "detectortype":
                    {
                        if (value == "advance")
                        {
                            
                        }
                    }
                    break;

                    case "bwmode":
                    {
                        if (value == "on")
                        {
                            rotaryEncoderMode = true;
                        }
                        else if (value == "off")
                        {
                            rotaryEncoderMode = false;
                        }
                    }
                    break;
                }
            }
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

                    }
                    //ctl.MasterVolume += delta;
                }
            }
        }

        public void SetSkypeVolume(float delta)
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

                    }
                }
            }
        }

        public void SetMasterVolume(float volume)
        {
            

            speakers.AudioEndpointVolume.MasterVolumeLevelScalar = volume;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            spotifies = System.Diagnostics.Process.GetProcessesByName("Spotify");
            skype = System.Diagnostics.Process.GetProcessesByName("Skype")[0];

            if (serialPort1 != null && serialPort1.IsOpen)
            {
                //serialPort1.Write(new byte[] { 1 }, 0, 1);
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            GC.Collect();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            
        }
    }

    
}
