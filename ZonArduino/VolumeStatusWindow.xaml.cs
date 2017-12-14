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
    public partial class VolumeStatusWindow : FadingWindow
    {

        Storyboard opacityAnimationStoryboard = new Storyboard();

        public VolumeStatusWindow()
        {
            InitializeComponent();

            

            this.Left = (Screen.PrimaryScreen.Bounds.Right - this.Width-20);
            this.Top = (Screen.PrimaryScreen.Bounds.Bottom - this.Height-20);

            this.RegisterName("PulsingBgColor", ((SolidColorBrush)FindResource("PulsingBgColor")));

            DoubleAnimation opacityAnimation = new DoubleAnimation();
            opacityAnimation.From = 0;
            opacityAnimation.To = 1;
            opacityAnimation.Duration = TimeSpan.FromMilliseconds(500);
            opacityAnimation.AutoReverse = true;
            opacityAnimation.RepeatBehavior = RepeatBehavior.Forever;
            opacityAnimation.EasingFunction = new ExponentialEase();
            Storyboard.SetTargetName(opacityAnimation, "PulsingBgColor");
            Storyboard.SetTargetProperty(opacityAnimation,
                new PropertyPath(SolidColorBrush.OpacityProperty));

            opacityAnimationStoryboard.Children.Add(opacityAnimation);

            opacityAnimationStoryboard.Begin(this);

            
        }

        public void SetMasterVol(float val)
        {
            ShowTemporarily();
            MasterVol.Dispatcher.InvokeAsync(delegate()
            {
                MasterVol.Margin = new Thickness(0, val.Map(0, 1, 237, 15), 0, 0);
            });
        }

        public void SetSpotifyVol(float val)
        {
            ShowTemporarily();
            SpotifyVol.Dispatcher.InvokeAsync(delegate()
            {
                Grid.SetColumn(SelectedGrid, 2);

                SpotifyVol.Margin = new Thickness(0, val.Map(0, 1, 237, 15), 0, 0);
            });
        }

        public void SetSkypeVol(float val)
        {
            ShowTemporarily();
            SkypeVol.Dispatcher.InvokeAsync(delegate()
            {
                Grid.SetColumn(SelectedGrid, 4);

                SkypeVol.Margin = new Thickness(0, val.Map(0, 1, 237, 15), 0, 0);
            });
        }
    }
}
