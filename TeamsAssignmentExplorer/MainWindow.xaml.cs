using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TeamsAssignmentExplorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string loadingHomeworkList = "(Loading homework list...)";
        const string selectFromDropDown = "(Please select from the drop-down list.)";

        public class FileListItem
        {
            public string FileName { get; set; }
            public string FullPath { get; set; }
            public ImageSource Icon { get; set; }
            public bool Obsolete { get; set; }
        }

        public class HomeworkListItem
        {
            public string Homework { get; set; }
            public string Display { get; set; }
            public bool WorkingFilesOnly { get; set; }

            public override string ToString() { return Homework; }
        }

        public class FormDataType : INotifyPropertyChanged
        {
            // Bindings with notification
            private string org;
            private string folder;
            private string homework;
            private bool show_working_files;

            public string Org
            {
                get { return org; }
                set { org = value; OnPropertyChanged("Org"); }
            }
            public string Folder
            {
                get { return folder; }
                set { folder = value; OnPropertyChanged("Folder"); }
            }
            public string Homework
            {
                get { return homework; }
                set { homework = value; OnPropertyChanged("Homework"); }
            }

            public bool ShowWorkingFiles
            {
                get { return show_working_files; }
                set { show_working_files = value; OnPropertyChanged("ShowWorkingFiles"); }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string name)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        readonly ObservableCollection<string> FolderList;
        readonly ObservableCollection<HomeworkListItem> HomeworkList;

        readonly FormDataType FormData;
        readonly ObservableCollection<FileListItem> FileList;

        public MainWindow()
        {
            InitializeComponent();

            FolderList = new ObservableCollection<string>();
            FolderComboBox.ItemsSource = FolderList;

            HomeworkList = new ObservableCollection<HomeworkListItem>();
            HomeworkComboBox.ItemsSource = HomeworkList;

            FormData = new FormDataType() { Folder = selectFromDropDown };
            DataContext = FormData;
            FormData.PropertyChanged += FormData_PropertyChanged;

            FileList = new ObservableCollection<FileListItem>();
            FileListListBox.ItemsSource = FileList;

            UpdateFolderList();
        }

        protected void RunAssociatedProgram(string path)
        {
            try
            {
                new Process
                {
                    StartInfo = new ProcessStartInfo(path) { UseShellExecute = true }
                }.Start();
            }
            catch (Win32Exception e)
            {
                MessageBox.Show(e.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected void OpenParentFolder(string pathToFile)
        {
            string path = System.IO.Path.GetDirectoryName(pathToFile);
            try
            {
                new Process
                {
                    StartInfo = new ProcessStartInfo("explorer.exe", '"' + path + '"')
                    {
                        UseShellExecute = true
                    }
                }.Start();
            }
            catch (Win32Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected void UpdateFolderList()
        {
            FolderList.Clear();
            foreach (string dir in DirAndFileScanner.GetFolderList())
            {
                FolderList.Add(dir);
            }
        }


        protected async Task UpdateHomeworkList()
        {
            FormData.Homework = loadingHomeworkList;
            HomeworkComboBox.IsEnabled = false;

            var list = await Task.Factory.StartNew(
                () => DirAndFileScanner.GetHomeworkList(FormData.Folder));
            HomeworkList.Clear();
            foreach (var item in list)
            {
                HomeworkList.Add(new HomeworkListItem
                {
                    Homework = item.Homework,
                    Display = item.WorkingFilesOnly ? item.Homework + "  (working files only)"
                                                    : item.Homework,
                    WorkingFilesOnly = item.WorkingFilesOnly
                });
            }

            FormData.Homework = selectFromDropDown;
            HomeworkComboBox.IsEnabled = true;
            return;
        }

        protected bool IsPreviousVersion(string prev, string next)
        {
            // TODO: explain logic with Working Files.
            string prevVersion = System.IO.Path.GetDirectoryName(prev);
            string prevUser = System.IO.Path.GetDirectoryName(prevVersion);

            string nextVersion = System.IO.Path.GetDirectoryName(next);
            string nextUser = System.IO.Path.GetDirectoryName(nextVersion);

            return prevUser == nextUser && prevVersion != nextVersion;
        }

        protected bool IsPreviousOrSameVersion(string prev, string next)
        {
            // TODO: explain logic with Working Files.
            string prevVersion = System.IO.Path.GetDirectoryName(prev);
            string prevUser = System.IO.Path.GetDirectoryName(prevVersion);

            string nextVersion = System.IO.Path.GetDirectoryName(next);
            string nextUser = System.IO.Path.GetDirectoryName(nextVersion);

            return prevUser == nextUser;
        }

        protected void MaybeUpdateFileList()
        {
            // TODO: investigate FormData.Homework can be empty if Folder is changed. This should
            // not happen.
            // Do not execute update if Homework is nothing.
            if (FormData.Homework.Trim() == "") return;
            if (FormData.Homework == loadingHomeworkList) return;
            if (FormData.Homework == selectFromDropDown) return;

            var allFiles = DirAndFileScanner.GetSubmittedAndWorkingFiles(FormData.Folder,
                                                                         FormData.Homework);
            List<string> files = allFiles.SubmittedFiles;
            if (FormData.ShowWorkingFiles || files.Count == 0)
                files.AddRange(allFiles.WorkingFiles);

            if (files.Count == 0)
                return;

            bool[] obsolete = new bool[files.Count];

            bool isObsolete = false;
            for (int i = files.Count - 2; i >= 0; --i)
            {
                isObsolete = isObsolete ? IsPreviousOrSameVersion(files[i], files[i + 1])
                                        : IsPreviousVersion(files[i], files[i + 1]);
                obsolete[i] = isObsolete;
            }

            FileList.Clear();
            for (int i = 0; i < files.Count; ++i)
            {
                string path = System.IO.Path.Combine(FormData.Folder, files[i]);
                BitmapSource icon = IconUtil.GetIconForExtension(System.IO.Path.GetExtension(path));
                FileList.Add(new FileListItem { FileName = files[i], FullPath = path,
                                                Icon = icon, Obsolete = obsolete[i] });
            }
        }

        protected void FormData_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Folder")
                _ = UpdateHomeworkList();
            else if (e.PropertyName == "Homework" || e.PropertyName == "ShowWorkingFiles")
                MaybeUpdateFileList();
        }

        protected void ListBoxItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            RunAssociatedProgram((((ListBoxItem)sender).Content as FileListItem).FullPath);
        }

        protected void ListBoxItem_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                RunAssociatedProgram((((ListBoxItem)sender).Content as FileListItem).FullPath);
        }

        private void ControlOpenItem_Click(object sender, EventArgs e)
        {
            var item = ((Control)sender).DataContext as FileListItem;
            FileListListBox.SelectedItem = item;
            RunAssociatedProgram(item.FullPath);
        }

        private void ControlOpenFolder_Click(object sender, EventArgs e)
        {
            var item = ((Control)sender).DataContext as FileListItem;
            FileListListBox.SelectedItem = item;
            OpenParentFolder(item.FullPath);
        }
    }
}
