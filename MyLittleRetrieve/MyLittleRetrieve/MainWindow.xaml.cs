using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MyLittleRetrieve.Annotations;
using MyLittleRetrieve.Engine;
using MyLittleRetrieve.Helpers;

namespace MyLittleRetrieve
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private const int ShowIndexCount = 10;

        private const int ShowResultsCount = 100;
        
        private string[] InputDocuments { get; set; }

        private string InputQueryFile { get; set; }

        private RetrievalEngine RetrievalEngine { get; set; }


        public IEnumerable<KeyValuePair<string, Dictionary<FileInfo, int>>> InvertedIndex => RetrievalEngine?.InvertedIndex.Take(ShowIndexCount);

        public IEnumerable<KeyValuePair<FileInfo, Dictionary<string, int>>> NonInvertedIndex => RetrievalEngine?.NonInvertedIndex.Take(ShowIndexCount);

        public bool EngineInitialized => RetrievalEngine?.Documents?.Any() ?? false;

        public List<TrecElement> Ranking { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DocumentsTextBox.Text = Properties.Settings.Default.DocumentsPath;
            if (Directory.Exists(DocumentsTextBox.Text))
            {
                InputDocuments = Directory.GetFiles(DocumentsTextBox.Text);
                InitializeButton.IsEnabled = true;
            }
            QueriesTextBox.Text = Properties.Settings.Default.QueriesPath;
            if (File.Exists(QueriesTextBox.Text))
            {
                InputQueryFile = QueriesTextBox.Text;
                QueryButton.IsEnabled = true;
            }
        }

        private void DocumentsTextBoxOnGotFocus(object sender, RoutedEventArgs e)
        {
            var path = FileHelper.SelectFolder(DocumentsTextBox.Text, "Documents");
            if (!Directory.Exists(path)) { return; }
            DocumentsTextBox.Text = path;
            Properties.Settings.Default.DocumentsPath = path;
            InputDocuments = Directory.GetFiles(path);
            InitializeButton.IsEnabled = true;
        }

        private void QueriesTextBoxOnGotFocus(object sender, RoutedEventArgs e)
        {
            var path = FileHelper.SelectFile(QueriesTextBox.Text, "Queries");
            if (!File.Exists(path)) { return; }
            QueriesTextBox.Text = path;
            Properties.Settings.Default.QueriesPath = path;
            InputQueryFile = path;
            QueryButton.IsEnabled = true;
        }

        private void DocumentsTextBoxOnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!Directory.Exists(DocumentsTextBox.Text)) { return; }
            InputDocuments = Directory.GetFiles(DocumentsTextBox.Text);
            Properties.Settings.Default.DocumentsPath = DocumentsTextBox.Text;
            InitializeButton.IsEnabled = true;
        }

        private void QueriesTextBoxOnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!File.Exists(QueriesTextBox.Text)) { return; }
            InputQueryFile = QueriesTextBox.Text;
            Properties.Settings.Default.QueriesPath = QueriesTextBox.Text;
            QueryButton.IsEnabled = true;
        }

        private void InitializeButtonClick(object sender, RoutedEventArgs e)
        {
            RetrievalEngine = new RetrievalEngine(InputDocuments);
            OnPropertyChanged(nameof(EngineInitialized));
            OnPropertyChanged(nameof(InvertedIndex));
            OnPropertyChanged(nameof(NonInvertedIndex));

        }

        private void QueryButtonClick(object sender, RoutedEventArgs e)
        {
            var file = new FileInfo(InputQueryFile);
            var content = file.ReadContent();
            Ranking = RetrievalEngine.ProcessQuery(file.Name, content).Take(ShowResultsCount).ToList();
            OnPropertyChanged(nameof(Ranking));
        }


        private void MainWindowOnClosed(object sender, EventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void CustomQueriesTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            QueryButton.IsEnabled = string.IsNullOrEmpty(CustomQueriesTextBox.Text);
            Ranking = RetrievalEngine.ProcessQuery(Guid.NewGuid().ToString(), CustomQueriesTextBox.Text)?.Take(ShowResultsCount).ToList();
            OnPropertyChanged(nameof(Ranking));
        }

        private void RowMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var row = sender as DataGridRow;
            if (row?.DataContext is TrecElement trecElement)
            {
                Process.Start(trecElement.File);
            }
        }
    }
}
