using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace LifeBucketList.App.Views;

public partial class ConfirmationWindow : Window
{
    public string QuestionTitle { get; }
    public string Message { get; }

    public ConfirmationWindow() : this("Bestätigen", string.Empty)
    {
    }

    public ConfirmationWindow(string title, string message)
    {
        QuestionTitle = title;
        Message = message;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnYesClick(object? sender, RoutedEventArgs e) => Close(true);

    private void OnNoClick(object? sender, RoutedEventArgs e) => Close(false);
}
