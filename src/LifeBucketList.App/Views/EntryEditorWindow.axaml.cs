using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using LifeBucketList.App.ViewModels;

namespace LifeBucketList.App.Views;

public partial class EntryEditorWindow : Window
{
    public EntryEditorWindow()
    {
        InitializeComponent();
        Opened += (_, _) => this.FindControl<TextBox>("TitleBox")?.Focus();
        KeyDown += OnKeyDown;
    }

    /// <summary>Enter submits the form, matching clicking "Speichern" — except inside the
    /// multiline Notiz field, where Enter must keep inserting a newline as usual.</summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || FocusManager?.GetFocusedElement() is TextBox { AcceptsReturn: true })
        {
            return;
        }

        e.Handled = true;
        OnSaveClick(this, new RoutedEventArgs());
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not EntryEditorViewModel viewModel)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(viewModel.Title))
        {
            var errorText = this.FindControl<TextBlock>("ValidationErrorText")!;
            errorText.Text = "Bitte gib einen Titel ein.";
            errorText.IsVisible = true;
            return;
        }

        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(false);

    private void OnClearDateClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is EntryEditorViewModel viewModel)
        {
            viewModel.OccurredOn = null;
        }
    }
}
