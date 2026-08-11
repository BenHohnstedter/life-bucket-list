using LifeBucketList.App.ViewModels;

namespace LifeBucketList.App.Services;

/// <summary>Abstraction over all window/dialog interactions, so ViewModels stay testable without a real UI.</summary>
public interface IDialogService
{
    /// <summary>Shows the entry editor dialog. Returns true if the user saved (editor properties are already updated).</summary>
    Task<bool> ShowEntryEditorAsync(EntryEditorViewModel editor);

    Task<bool> ShowConfirmationAsync(string title, string message);

    Task ShowErrorAsync(string title, string message);

    /// <summary>Opens a "Save As" dialog for the JSON export. Returns the chosen path, or null if cancelled.</summary>
    Task<string?> PickExportFilePathAsync(string suggestedFileName);

    /// <summary>Opens an "Open" dialog for the JSON import. Returns the chosen path, or null if cancelled.</summary>
    Task<string?> PickImportFilePathAsync();
}
