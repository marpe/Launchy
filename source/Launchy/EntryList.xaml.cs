using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace Launchy
{
    /// <summary>
    /// Interaction logic for EntryList.xaml
    /// </summary>
    public partial class EntryList : Window, INotifyPropertyChanged
    {
        public static ObservableCollection<Entry> Entries
        {
            get;
            set;
        }

        public bool hasItemSelected { get { return lbEntries.SelectedItem != null; } }

        public EntryList(ObservableCollection<Entry> entries)
        {
            InitializeComponent();

            DataContext = this;

            Entries = entries;
            lbEntries.SelectionChanged += (x, y) => { PropertyChanged(this, new PropertyChangedEventArgs("hasItemSelected")); };
        }

        private void btnDelete_Click_1(object sender, RoutedEventArgs e)
        {
            var entry = (Entry)lbEntries.SelectedItem;
            Entries.Remove(entry);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void btnSave_Click_1(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance().Save();
            System.Windows.MessageBox.Show(Entries.Count + " entries saved!", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnAdd_Click_1(object sender, RoutedEventArgs e)
        {
            MainWindow.AddNewFileEntry();
        }

        private void btnEdit_Click_1(object sender, RoutedEventArgs e)
        {
            EditEntry edit = new EditEntry((Entry)lbEntries.SelectedItem);
            edit.ShowDialog();
        }

        private void btnClose_Click_1(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnAddDir_Click_1(object sender, RoutedEventArgs e)
        {
            MainWindow.AddNewDirectoryEntry();
        }
    }
}
