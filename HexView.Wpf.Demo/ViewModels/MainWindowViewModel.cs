namespace HexViewDemo.ViewModels
{
    using System;
    using System.IO;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using Wpf.Ui.Appearance;

    /// <summary>
    /// ViewModel for the MainWindow of HexViewer Demo.
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private BinaryReader reader;

        [ObservableProperty]
        private ApplicationTheme currentTheme = ApplicationTheme.Light;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
        /// </summary>
        public MainWindowViewModel()
        {
            // Generate random data so we display something right out of the box without forcing the user to open a file
            var rand = new Random();

            // 10 MB of random data
            var bytes = new byte[10 * 1024 * 1024];
            rand.NextBytes(bytes);

            Reader = new BinaryReader(new MemoryStream(bytes));
        }

        [RelayCommand]
        private void OpenFile()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();

            if (openFileDialog.ShowDialog() == true)
            {
                var file = File.Open(openFileDialog.FileName, FileMode.Open);
                Reader = new BinaryReader(file);
            }
        }

        [RelayCommand]
        private void SetTheme(string theme)
        {
            if (Enum.TryParse<ApplicationTheme>(theme, true, out var appTheme))
            {
                CurrentTheme = appTheme;
                ApplicationThemeManager.Apply(appTheme);
            }
        }

        [RelayCommand]
        private void Exit()
        {
            System.Windows.Application.Current.MainWindow?.Close();
        }
    }
}
