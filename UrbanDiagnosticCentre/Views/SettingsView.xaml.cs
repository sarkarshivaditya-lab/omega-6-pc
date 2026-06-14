using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using UrbanDiagnosticCentre.ViewModels;

namespace UrbanDiagnosticCentre.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void BrowseLogo_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm) return;

        var dialog = new OpenFileDialog
        {
            Title           = "Select Logo Image",
            Filter          = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All Files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog() == true)
            vm.LogoPath = dialog.FileName;
    }

    private void BrowseReportsFolder_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm) return;

        var dialog = new OpenFolderDialog
        {
            Title              = "Select Reports Storage Folder",
            InitialDirectory   = vm.ReportsRootPath
        };

        if (dialog.ShowDialog() == true)
            vm.ReportsRootPath = dialog.FolderName;
    }

    private void BrowseBackupsFolder_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm) return;

        var dialog = new OpenFolderDialog
        {
            Title              = "Select Default Backup Folder",
            InitialDirectory   = vm.BackupsRootPath
        };

        if (dialog.ShowDialog() == true)
            vm.BackupsRootPath = dialog.FolderName;
    }

    private void BrowseLabSignature_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm) return;

        var dialog = new OpenFileDialog
        {
            Title           = "Select Lab Incharge Signature Image",
            Filter          = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All Files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog() == true)
            vm.LabInchargeSignaturePath = dialog.FileName;
    }

    private void BrowseConsultantSignature_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm) return;

        var dialog = new OpenFileDialog
        {
            Title           = "Select Consultant Pathologist Signature Image",
            Filter          = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All Files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog() == true)
            vm.ConsultantPathologistSignaturePath = dialog.FileName;
    }

    private async void ChangePassword_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm) return;

        var (success, message) = await vm.TryChangePasswordAsync(
            PwCurrent.Password,
            PwNew.Password,
            PwConfirm.Password);

        vm.PwChangeIsError = !success;
        vm.PwChangeStatus  = message;

        if (success)
        {
            PwCurrent.Clear();
            PwNew.Clear();
            PwConfirm.Clear();
        }
    }
}
