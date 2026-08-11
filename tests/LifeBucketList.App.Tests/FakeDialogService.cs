using LifeBucketList.App.Services;
using LifeBucketList.App.ViewModels;

namespace LifeBucketList.App.Tests;

/// <summary>Test double for <see cref="IDialogService"/>: simulates user input without any real windows.</summary>
public sealed class FakeDialogService : IDialogService
{
    public Func<EntryEditorViewModel, bool> EntryEditorHandler { get; set; } = _ => false;
    public Func<string, string, bool> ConfirmationHandler { get; set; } = (_, _) => true;
    public string? ExportPath { get; set; }
    public string? ImportPath { get; set; }

    public List<(string Title, string Message)> Errors { get; } = new();

    public Task<bool> ShowEntryEditorAsync(EntryEditorViewModel editor) => Task.FromResult(EntryEditorHandler(editor));

    public Task<bool> ShowConfirmationAsync(string title, string message) => Task.FromResult(ConfirmationHandler(title, message));

    public Task ShowErrorAsync(string title, string message)
    {
        Errors.Add((title, message));
        return Task.CompletedTask;
    }

    public Task<string?> PickExportFilePathAsync(string suggestedFileName) => Task.FromResult(ExportPath);

    public Task<string?> PickImportFilePathAsync() => Task.FromResult(ImportPath);
}
