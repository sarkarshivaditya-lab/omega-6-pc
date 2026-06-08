using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UrbanDiagnosticCentre.ViewModels;

namespace UrbanDiagnosticCentre.Views;

public partial class PatientReportView : UserControl
{
    public PatientReportView()
    {
        InitializeComponent();
    }

    // Ctrl+Enter → Complete Report  |  Escape → back to Dashboard  |  Enter → next field
    private void PatientReport_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not PatientReportViewModel vm) return;

        if (e.Key == Key.Escape && !vm.IsSaving)
        {
            if (vm.NavigateToDashboardCommand.CanExecute(null))
                vm.NavigateToDashboardCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter &&
            (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            if (vm.CompleteReportCommand.CanExecute(null))
                vm.CompleteReportCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // Enter alone on a single-line TextBox → advance to next Tab stop
        if (e.Key == Key.Enter &&
            Keyboard.Modifiers == ModifierKeys.None &&
            e.OriginalSource is TextBox tb &&
            !tb.AcceptsReturn)
        {
            tb.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            e.Handled = true;
        }
    }
}
