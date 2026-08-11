using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using LifeBucketList.App.ViewModels;
using LifeBucketList.App.Views;

namespace LifeBucketList.App.Services;

/// <summary>Real, window-based implementation of <see cref="IDialogService"/> for the desktop app.</summary>
public sealed class AvaloniaDialogService : IDialogService
{
    private static Window Owner =>
        (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow
        ?? throw new InvalidOperationException("Kein Hauptfenster verfügbar.");

    public async Task<bool> ShowEntryEditorAsync(EntryEditorViewModel editor)
    {
        var window = new EntryEditorWindow { DataContext = editor };
        return await window.ShowDialog<bool>(Owner);
    }

    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var window = new ConfirmationWindow(title, message);
        return await window.ShowDialog<bool>(Owner);
    }

    public async Task ShowErrorAsync(string title, string message)
    {
        var window = new MessageWindow(title, message);
        await window.ShowDialog(Owner);
    }

    public async Task<string?> PickExportFilePathAsync(string suggestedFileName)
    {
        var options = new FilePickerSaveOptions
        {
            Title = "Backup exportieren",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "json",
            FileTypeChoices = new[] { new FilePickerFileType("JSON-Datei") { Patterns = new[] { "*.json" } } },
        };

        var file = await Owner.StorageProvider.SaveFilePickerAsync(options);
        return file?.TryGetLocalPath();
    }

    public async Task<string?> PickImportFilePathAsync()
    {
        var options = new FilePickerOpenOptions
        {
            Title = "Backup importieren",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("JSON-Datei") { Patterns = new[] { "*.json" } } },
        };

        var files = await Owner.StorageProvider.OpenFilePickerAsync(options);
        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }
}
