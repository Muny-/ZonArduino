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
using System.Windows.Interop;
using System.Windows.Media.Animation;

namespace ZonArduino
{
    /// <summary>
    /// Interaction logic for VolumeStatusWindow.xaml
    /// </summary>
    public partial class TrackStatusWindow : FadingWindow
    {

        Storyboard opacityAnimationStoryboard = new Storyboard();

        public TrackStatusWindow()
        {
            InitializeComponent();

            

            this.Left = (Screen.PrimaryScreen.Bounds.Right - this.Width - 20);
            this.Top = (Screen.PrimaryScreen.Bounds.Bottom - this.Height - 20);

            
        }

        public void NextTrack()
        {
            StatusLabel.Dispatcher.Invoke(delegate()
            {
                this.StatusLabel.Content = "NEXT TRACK ➡";
            });
            
            this.ShowTemporarily();
        }

        public void PreviousTrack()
        {
            StatusLabel.Dispatcher.Invoke(delegate()
            {
                this.StatusLabel.Content = "PREVIOUS TRACK ⬅";
            });

            this.ShowTemporarily();
        }
    }
}
