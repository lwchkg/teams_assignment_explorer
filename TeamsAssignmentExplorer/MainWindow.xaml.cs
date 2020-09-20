using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
        const string selectFromDropDown = "(Please select from the drop-down list.)";

        public class FileListItem
        {
            public string FileName { get; set; }
            public string FullPath { get; set; }
            public ImageSource Icon { get; set; }
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

        protected void UpdateFolderList()
        {
            FolderList.Clear();
            foreach (string dir in DirAndFileScanner.GetFolderList())
            {
                FolderList.Add(dir);
            }
        }


        protected void UpdateHomeworkList()
        {
            var list = DirAndFileScanner.GetHomeworkList(FormData.Folder);
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
            return;
        }

        protected void MaybeUpdateFileList()
        {
            List<string> files = DirAndFileScanner.GetSubmittedFiles(FormData.Folder,
                                                                     FormData.Homework);
            if (FormData.ShowWorkingFiles || files.Count == 0)
                files.AddRange(DirAndFileScanner.GetWorkingFiles(FormData.Folder,
                                                                 FormData.Homework));

            if (files.Count == 0)
                return;

            FileList.Clear();

            foreach (var file in files)
            {
                string path = System.IO.Path.Combine(FormData.Folder, file);
                using (var ico = IconTools.GetIconForExtension(System.IO.Path.GetExtension(path),
                                                               ShellIconSize.SmallIcon))
                {
                    var image = Imaging.CreateBitmapSourceFromHIcon(
                        ico.Handle, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(16, 16));
                    FileList.Add(new FileListItem { FileName = file, FullPath = path,
                                                    Icon = image });
                }
            }
        }

        protected void FormData_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Folder")
                UpdateHomeworkList();
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

        protected void ContextMenuOpenItem_Click(object sender, EventArgs e)
        {
            RunAssociatedProgram((((MenuItem)sender).DataContext as FileListItem).FullPath);
        }

        protected void ContextMenuOpenFolder_Click(object sender, EventArgs e)
        {
            string path = System.IO.Path.GetDirectoryName(
                (((MenuItem)sender).DataContext as FileListItem).FullPath);
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
    }
}
