using ManagedWinapi.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Serialization;

namespace Launchy
{
    public class Entry : IEntry, INotifyPropertyChanged
    {
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public delegate void ExecuteDelegate();

        public event ExecuteDelegate OnExecute;
        public event PropertyChangedEventHandler PropertyChanged;

        string _title;
        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                _title = value;
                propertyChanged("Title");                
            }
        }

        Brush _background;
        [XmlIgnore]
        public Brush Background
        {
            get
            {
                return _background;
            }
            set
            {
                _background = value;
                propertyChanged("Background");
            }
        }

        string _command;
        public string Command
        {
            get { return _command; }
            set
            {
                _command = value;
                if (!string.IsNullOrWhiteSpace(Command))
                {
                    try
                    {
                        Icon = System.Drawing.Icon.ExtractAssociatedIcon(Command).ToImageSource();
                    }
                    catch (Exception)
                    {

                    }
                }
                propertyChanged("Command");
            }
        }

        ImageSource _icon;
        [XmlIgnore]
        public ImageSource Icon
        {
            get
            {
                return _icon;
            }
            set
            {
                _icon = value;
                propertyChanged("Icon");
            }
        }

        public Entry()
        {
            Title = "";
            Command = "";
            Icon = null;
            Background = Brushes.Transparent;

            OnExecute = startProcess;
        }

        public Entry(string title, string cmd, bool restore = false)
        {
            Title = title;
            Command = cmd;
            Background = Brushes.Transparent;

            if (restore)
                OnExecute = restoreProcess;
            else
                OnExecute = startProcess;
        }

        private void propertyChanged(string prop)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        private void restoreProcess()
        {
            var win = MyUtilities.FindWindow(Title);
            if (win != null)
            {
                if (win.WindowState == System.Windows.Forms.FormWindowState.Minimized)
                {
                    int SW_RESTORE = 9;
                    ShowWindow(win.HWnd, SW_RESTORE);
                }

                SystemWindow.ForegroundWindow = win;
            }
        }

        private void startProcess()
        {
            if (string.IsNullOrEmpty(Command))
                restoreProcess();
            else
                Process.Start(Command);
        }

        public void Execute()
        {
            if (OnExecute != null)
                OnExecute();
        }

    }
}
