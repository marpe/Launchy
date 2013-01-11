﻿using System;
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
using System.Windows.Forms;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.ComponentModel;
using System.Xml.Serialization;
using System.IO;
using System.Windows.Threading;

namespace Launchy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    using KeyEventArgs = System.Windows.Input.KeyEventArgs;
    using Ico = System.Drawing.Icon;
    using ManagedWinapi.Windows;
    using System.Management;
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        NotifyIcon notifyIcon;
        KeyboardHook hook;
        ObservableCollection<Entry> entries { get; set; }
        public ObservableCollection<Entry> autoComplete { get; set; }
        DispatcherTimer timer;

        public Visibility hasAutoCompleteItems
        {
            get
            {
                return autoComplete.Any() ? Visibility.Visible : Visibility.Hidden;
            }
        }

        public static bool isCaseSensitive = false;

        public MainWindow()
        {
            InitializeComponent();

            autoComplete = new ObservableCollection<Entry>();

            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location); // new System.Drawing.Icon(@"C:\icon.ico");
            notifyIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(notifyIcon_MouseClick);
            notifyIcon.MouseDoubleClick += notifyIcon_MouseDoubleClick;
            notifyIcon.Visible = true;
            hook = new KeyboardHook();
            hook.RegisterHotKey(ModifierKeys.Control, Keys.Space);
            hook.KeyPressed += hook_KeyPressed;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMinutes(5);
            timer.Tick += timer_Tick;
            timer.Start();

            DataContext = this;

            load();

            autoComplete.CollectionChanged += AutoComplete_CollectionChanged;

            //Custom entries
            var c = new[] {
                new Entry() { Title = "Mouse", Command = "main.cpl" },
                new Entry() { Title = "Add/Remove Programs", Command = "appwiz.cpl" },
                new Entry() { Title = "Date/Time Properties", Command = "timedate.cpl" },
                new Entry() { Title = "Display Properties", Command = "desk.cpl" },
                new Entry() { Title = "Sound Properties", Command = "mmsys.cpl" },
             //   new Entry() { Title = "Sky Drive", Command = @"C:\Users\marpe\SkyDrive\Dokument" }, absolute paths won't turn out well...
            };

            var cm = new System.Windows.Forms.ContextMenu();
            cm.MenuItems.Add("Show Entries", (x, y) => { showList(); });
            cm.MenuItems.Add("Add File Entry", (x, y) => { AddNewFileEntry(); });
            cm.MenuItems.Add("Add Directory Entry", (x, y) => { AddNewDirectoryEntry(); });
            cm.MenuItems.Add("Close", (x, y) => { System.Windows.Application.Current.Shutdown(); });
            notifyIcon.ContextMenu = cm;

            foreach (var e in c)
                AddEntry(e, false);

            _instance = this;
        }

        void notifyIcon_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
      //      showList();
        }

        EntryList list = null;

        void showList()
        {
            if (list == null)
            {
                list = new EntryList(entries);
                list.ShowDialog();
                list = null;
            }
        }

        void timer_Tick(object sender, EventArgs e)
        {
            addRunning();
            Save();
        }

        void AutoComplete_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            PropertyChanged(this, new PropertyChangedEventArgs("hasAutoCompleteItems"));
        }

        void hook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            WindowState = System.Windows.WindowState.Normal;
            Activate();
            tbInput.Focus();
            tbInput.SelectAll();
        }

        private void notifyIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            WindowState = System.Windows.WindowState.Normal;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (e.Key == Key.Escape)
            {
                WindowState = System.Windows.WindowState.Minimized;
            }
            else if (e.Key == Key.F1)
            {
                showList();
            }
        }

        private void tbInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                lbAutoComplete.Focus();
            }
        }

        private void lbAutoComplete_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up && lbAutoComplete.SelectedIndex == 0)
            {
                tbInput.Focus();
            }
            else if (e.Key == Key.Down)
            {
            }
            else if (e.Key == Key.Enter)
            {
                var entry = (Entry)lbAutoComplete.SelectedItem;
                Execute(entry);
            }
            else
            {
                
            }
        }

        public void Execute(Entry e)
        {
            try
            {
                e.Execute();
                tbInput.Text = string.Empty;
                WindowState = System.Windows.WindowState.Minimized;
            }
            catch (Exception ex)
            {
                var error = string.Format("{0} ({1})", ex.Message, e.Command);
                System.Windows.MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                if (System.Windows.MessageBox.Show("Do you want to delete the entry?", Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    entries.Remove(e);
                }
                //throw;
            }
        }

        private void tbInput_KeyDown(object sender, KeyEventArgs e)
        {
            var input = tbInput.Text;

            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(input))
            {
                if (lbAutoComplete.Items.Count > 0)
                {
                    var entry = (Entry)lbAutoComplete.Items[0];
                    Execute(entry);
                }
                else
                {
                    OpenFileDialog ofd = new OpenFileDialog();
                    if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        input = ofd.FileName;
                        var entry = new Entry(System.IO.Path.GetFileName(input), input);
                        AddEntry(entry);
                        Execute(entry);
                    }
                }

                tbInput.Clear();
            }
        }

        private void tbInput_KeyUp(object sender, KeyEventArgs e)
        {

        }

        private void tbInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            var input = tbInput.Text;
            autoComplete.Clear();

            if (string.IsNullOrWhiteSpace(input))
                return;

            var auto = entries.Where(x => (x.Title + " " + x.Command).MyStartsWith(input)).ToList();

            //Add running windows
            var processes = Process.GetProcesses();
            foreach (var proc in processes)
            {
                try
                {
                    if (proc.MainWindowTitle.MyStartsWith(input))
                    {
                        var filename = proc.MainModule.FileName;
                        var title = proc.MainWindowTitle;
                        auto.Add(new Entry(title, filename) { Background = Brushes.CornflowerBlue });
                    }
                }

                catch (Win32Exception)
                {
                    //don't add if it's a 32 bit launchy and a 64 bit app (do nothing)
                }
            }

            auto.Reverse();

            foreach (var entry in auto)
                autoComplete.Add(entry);


            lbAutoComplete.SelectedIndex = 0;
        }

        private void window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                ShowInTaskbar = false;
            }
            else if (this.WindowState == WindowState.Normal)
            {
                Activate();
                ShowInTaskbar = true;
            }
        }

        private void window_Activated(object sender, EventArgs e)
        {
            addRunning();
            tbInput.Focus();
        }

        private string GetMainModuleFilepath(int processId)
        {
            string wmiQueryString = "SELECT ProcessId, ExecutablePath FROM Win32_Process WHERE ProcessId = " + processId;
            using (var searcher = new ManagementObjectSearcher(wmiQueryString))
            {
                using (var results = searcher.Get())
                {
                    ManagementObject mo = results.Cast<ManagementObject>().FirstOrDefault();
                    if (mo != null)
                    {
                        return (string)mo["ExecutablePath"];
                    }
                }
            }
            return null;
        }

        private void addRunning()
        {
            var processes = Process.GetProcesses();
            foreach (var proc in processes)
            {
                try
                {
                    if (proc.MainWindowTitle.Length > 0)
                    {
                        var filename = GetMainModuleFilepath(proc.Id); //proc.MainModule.FileName;
                        var title = proc.ProcessName;
                        AddEntry(new Entry(title, filename), false);
                    }
                }

                catch (Win32Exception e)
                {
                    //do nothing
                }
            }
        }

        public void AddEntry(Entry e, bool showError = true)
        {
            var e2 = entries.FirstOrDefault(x => x.Title.Equals(e.Title, StringComparison.CurrentCultureIgnoreCase));
            if (e2 == null)
                entries.Add(e);
            else if(showError)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("An entry with the same title/command already exists");
                sb.AppendLine("Title = " + e2.Title + ", " + e.Title);
                sb.AppendLine("Command = " + e2.Command + ", " + e.Command);
                System.Windows.MessageBox.Show(sb.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            notifyIcon.Visible = false;
        }

        const string fileName = "entries.xml";

        private XmlSerializer xml = new XmlSerializer(typeof(ObservableCollection<Entry>));

        public void Save()
        {
            FileStream fs = new FileStream(fileName, FileMode.Create);
            var list = new ObservableCollection<Entry>(entries.OrderBy(x => x.Title));
            xml.Serialize(fs, list);
            fs.Close();
        }

        private void load()
        {
            if (File.Exists(fileName))
            {
                FileStream fs = null;
                try
                {
                    fs = new FileStream(fileName, FileMode.Open);
                    entries = (ObservableCollection<Entry>)xml.Deserialize(fs);
                }
                catch (Exception e)
                {
                    entries = new ObservableCollection<Entry>();
                    System.Windows.MessageBox.Show("Couldn't open list of entries (" + e.Message + ")", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    fs.Close();
                }
            }
            else
            {
                entries = new ObservableCollection<Entry>();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void window_Deactivated(object sender, EventArgs e)
        {
            var win = new ManagedWinapi.Windows.SystemWindow(new WindowInteropHelper(this).Handle);
            if ((win.Style & WindowStyleFlags.DISABLED) != WindowStyleFlags.DISABLED)
            {
                WindowState = System.Windows.WindowState.Minimized;
                tbInput.Text = string.Empty;
            }
        }

        private static void addNewEntry(string title, string cmd)
        {
            
            var entry = new Entry(title, cmd);
            MainWindow.Instance().AddEntry(entry);
        }

        public static void AddNewFileEntry()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var input = ofd.FileName;
                addNewEntry(System.IO.Path.GetFileName(input), input);
            }
        }

        public static void AddNewDirectoryEntry()
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var input = fbd.SelectedPath;
                addNewEntry(System.IO.Path.GetFileName(input), input);
            }
        }

        public bool HasEntryWithTitle(string title)
        {
            return entries.Any(x => x.Title.Equals(title, StringComparison.CurrentCultureIgnoreCase));
        }

        private static MainWindow _instance;

        public static MainWindow Instance()
        {
            return _instance;
        }

        private void lbAutoComplete_MouseDoubleClick_1(object sender, MouseButtonEventArgs e)
        {
            if (lbAutoComplete.SelectedItem != null)
            {
                var entry = (Entry)lbAutoComplete.SelectedItem;
                Execute(entry);
            }
        }

        private void lbAutoComplete_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (lbAutoComplete.SelectedItem != null)
            {
                var entry = (Entry)lbAutoComplete.SelectedItem;
                var menu =  new System.Windows.Controls.ContextMenu();
                var mi1 = new System.Windows.Controls.MenuItem();
                mi1.Header = "Open Folder";
                mi1.Click += (x, y) =>
                {
                    Process.Start(System.IO.Path.GetDirectoryName(entry.Command));
                };

                var mi2 = new System.Windows.Controls.MenuItem();
                mi2.Header = "Edit";
                mi2.Click += (x, y) =>
                {
                    var edit = new EditEntry(entry);
                    edit.ShowDialog();
                };

                menu.Items.Add(mi1);
                menu.Items.Add(mi2);

                menu.PlacementTarget = lbAutoComplete;
                menu.IsOpen = true;
            }
        }
    }

    public static class MyUtilities
    {
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        public static ImageSource ToImageSource(this System.Drawing.Icon icon)
        {
            System.Drawing.Bitmap bitmap = icon.ToBitmap();
            IntPtr hBitmap = bitmap.GetHbitmap();

            ImageSource wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            if (!DeleteObject(hBitmap))
            {
                throw new Win32Exception();
            }

            return wpfBitmap;
        }

        public static bool MyStartsWith(this string a, string b)
        {
            var split = new[] { '.', ' ', '\\', '/', '(', ')', '[', ']', '"', '\'' };
            var parts = a.ToLower().Split(split);
            b = b.ToLower();
            return parts.Any(x => x.StartsWith(b));
        }
    }
}
