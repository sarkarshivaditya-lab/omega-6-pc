using System.Windows.Controls;
using Microsoft.Win32;
using UrbanDiagnosticCentre.ViewModels;

namespace UrbanDiagnosticCentre.Views;

public partial class BackupView : UserControl
{
    public BackupView()
    {
        InitializeComponent();
    }

    private void BrowseDestination_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is not BackupViewModel vm) return;

        var dialog = new OpenFolderDialog
        {
            Title = "Select Backup Destination Folder",
            InitialDirectory = vm.BackupDestinationFolder
        };

        if (dialog.ShowDialog() == true)
            vm.BackupDestinationFolder = dialog.FolderName;
    }

    private void BrowseRestoreFile_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is not BackupViewModel vm) return;

        var dialog = new OpenFileDialog
        {
            Title  = "Select UDC Backup File",
            Filter = "UDC Backup (*.zip)|*.zip|All Files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog() == true)
            vm.RestoreSourcePath = dialog.FileName;
    }
}
