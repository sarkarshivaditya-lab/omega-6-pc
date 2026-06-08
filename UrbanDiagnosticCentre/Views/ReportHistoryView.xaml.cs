using System.Windows.Controls;
using System.Windows.Input;
using UrbanDiagnosticCentre.ViewModels;

namespace UrbanDiagnosticCentre.Views;

public partial class ReportHistoryView : UserControl
{
    public ReportHistoryView()
    {
        InitializeComponent();
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is ReportHistoryViewModel vm)
            vm.RunSearch();
    }
}
