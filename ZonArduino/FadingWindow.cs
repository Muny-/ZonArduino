using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Threading;

namespace ZonArduino
{
    public class FadingWindow : System.Windows.Window
    {
        DispatcherTimer opacityTimer = new DispatcherTimer();
        DispatcherTimer timeoutTimer = new DispatcherTimer();

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            //Set the window style to noactivate.
            WindowInteropHelper helper = new WindowInteropHelper(this);
            SetWindowLong(helper.Handle, GWL_EXSTYLE,
                GetWindowLong(helper.Handle, GWL_EXSTYLE) | WS_EX_NOACTIVATE);
        }

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        public FadingWindow()
        {
            opacityTimer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            opacityTimer.Tick += opacityTimer_Tick;

            timeoutTimer.Interval = new TimeSpan(0, 0, 0, 1, 0);
            timeoutTimer.Tick += timeoutTimer_Tick;
            timeoutTimer.Start();
        }

        void timeoutTimer_Tick(object sender, EventArgs e)
        {
            if (doFadeOut && (DateTime.Now - time_set) >= TimeSpan.FromMilliseconds(800))
            {
                doFadeOut = false;
                SetOpacity(0);
            }
        }

        float target_opacity = 0;
        DateTime time_set = DateTime.Now;
        bool doFadeOut = false;

        void opacityTimer_Tick(object sender, EventArgs e)
        {
            this.time_set = DateTime.Now;

            if (this.target_opacity == 1)
                this.Show();

            if (this.Opacity > target_opacity)
            {
                if (this.Opacity - target_opacity >= 0.04)
                    this.Opacity -= 0.04;
                else
                    this.Opacity = target_opacity;
            }
            else if (this.Opacity < target_opacity)
            {
                if (target_opacity - this.Opacity >= 0.04)
                    this.Opacity += 0.04;
                else
                    this.Opacity = target_opacity;
            }
            else
            {
                opacityTimer.Stop();
                if (target_opacity == 0)
                    this.Hide();
            }
        }

        void SetOpacity(float target)
        {
            this.target_opacity = target;
            opacityTimer.Start();
        }

        public void ShowTemporarily()
        {
            SetOpacity(1);

            doFadeOut = true;
        }
    }
}
