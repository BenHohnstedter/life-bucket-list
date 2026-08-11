using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace LifeBucketList.App.Views;

public partial class MessageWindow : Window
{
    public string MessageTitle { get; }
    public string Message { get; }

    public MessageWindow() : this(string.Empty, string.Empty)
    {
    }

    public MessageWindow(string title, string message)
    {
        MessageTitle = title;
        Message = message;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnOkClick(object? sender, RoutedEventArgs e) => Close();
}
