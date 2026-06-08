using System.Windows;

namespace UrbanDiagnosticCentre.Services;

public static class DialogService
{
    public static void ShowError(string message, string title = "Error")
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    public static void ShowInfo(string message, string title = "Information")
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public static void ShowWarning(string message, string title = "Warning")
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);

    public static bool Confirm(string message, string title = "Confirm")
        => MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public static void ShowPdfNotFound(string reportCode)
        => ShowError(
            $"The PDF for report {reportCode} could not be found on disk.\n\n" +
            "It may have been moved or deleted. Ask your administrator to check the reports folder.",
            "PDF Not Found");

    public static void ShowPdfOpenError(string reportCode, string detail)
        => ShowError(
            $"Could not open the PDF for report {reportCode}.\n\n" +
            $"Reason: {detail}\n\n" +
            "Ensure a PDF viewer (such as Adobe Acrobat Reader) is installed and try again.",
            "Cannot Open PDF");

    public static void ShowSaveError(Exception ex)
        => ShowError(
            "The report could not be saved. Please check that the reports folder is accessible " +
            "and try again.\n\n" +
            $"Details: {ex.Message}",
            "Save Failed");
}
