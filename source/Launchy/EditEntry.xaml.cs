using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Launchy
{
    /// <summary>
    /// Interaction logic for EditEntry.xaml
    /// </summary>
    public partial class EditEntry : Window, INotifyPropertyChanged
    {
        string _entryTitle;
        public string EntryTitle
        {
            get
            {
                return _entryTitle;
            }
            set
            {
                _entryTitle = value;
                if(PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("EntryTitle"));
            }
        }

        string _entryCommand;
        public string EntryCommand
        {
            get
            {
                return _entryCommand;
            }
            set
            {
                _entryCommand = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("EntryCommand"));                
            }
        }

        private Entry _original;

        public EditEntry(Entry e)
        {
            InitializeComponent();
            _original = e;
            EntryTitle = _original.Title;
            EntryCommand = _original.Command;
            DataContext = this;
        }

        private void btnBrowse_Click_1(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var fn = ofd.FileName;
                EntryCommand = fn;
            }
        }

        private void btnSave_Click_1(object sender, RoutedEventArgs e)
        {
            if (_original.Title.Equals(EntryTitle, StringComparison.CurrentCultureIgnoreCase) || MainWindow.Instance().HasEntryWithTitle(EntryTitle) == false)
            {
                _original.Title = EntryTitle;
                _original.Command = EntryCommand;
                Close();
            }
            else
            {
                System.Windows.MessageBox.Show("An entry with the same title already exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click_1(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
