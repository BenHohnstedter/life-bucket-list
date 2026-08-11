using System.ComponentModel;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using LifeBucketList.App.Services;
using LifeBucketList.App.ViewModels;
using LifeBucketList.App.Views;
using LifeBucketList.Data;
using LifeBucketList.Domain.Repositories;
using LifeBucketList.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LifeBucketList.App;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = BuildServiceProvider();
            _serviceProvider = services;

            var connectionFactory = services.GetRequiredService<SqliteConnectionFactory>();
            connectionFactory.InitializeAsync().GetAwaiter().GetResult();

            var mainViewModel = services.GetRequiredService<MainWindowViewModel>();
            mainViewModel.InitializeAsync().GetAwaiter().GetResult();

            InitializeTheme(mainViewModel);
            mainViewModel.PropertyChanged += OnMainViewModelPropertyChanged;

            desktop.MainWindow = new MainWindow { DataContext = mainViewModel };
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>Resolves "Default" (follow-OS) down to a concrete Light/Dark variant once, and syncs
    /// the toggle to it. Without this, the toggle always started at its default (off/light) even when
    /// the OS/app was actually rendering in dark mode, requiring an extra click to get back in sync.</summary>
    public void InitializeTheme(MainWindowViewModel mainViewModel)
    {
        var isDark = ActualThemeVariant == ThemeVariant.Dark;
        RequestedThemeVariant = isDark ? ThemeVariant.Dark : ThemeVariant.Light;
        mainViewModel.IsDarkTheme = isDark;
    }

    private static void OnMainViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainWindowViewModel.IsDarkTheme) || sender is not MainWindowViewModel vm)
        {
            return;
        }

        Current!.RequestedThemeVariant = vm.IsDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddSingleton(_ => new SqliteConnectionFactory(SqliteConnectionFactory.GetDefaultDatabasePath()));
        services.AddSingleton<ICategoryRepository, SqliteCategoryRepository>();
        services.AddSingleton<IEntryRepository, SqliteEntryRepository>();
        services.AddSingleton<IBackupService, JsonBackupService>();
        services.AddSingleton<IDialogService, AvaloniaDialogService>();

        services.AddSingleton<HttpClient>();
        services.AddSingleton<IApiKeyProvider>(_ => new ApiKeyProvider(ApiKeyProvider.GetDefaultFilePath(), AppContext.BaseDirectory));
        services.AddSingleton<ITwitchTokenProvider>(sp =>
            new TwitchTokenProvider(sp.GetRequiredService<HttpClient>(), sp.GetRequiredService<IApiKeyProvider>()));
        services.AddSingleton<ICoverSearchService, TmdbIgdbCoverSearchService>();
        services.AddSingleton<ICoverImageCache>(sp =>
            new CoverImageCache(sp.GetRequiredService<HttpClient>(), CoverImageCache.GetDefaultCacheDirectory()));

        services.AddSingleton<MainWindowViewModel>();

        return services.BuildServiceProvider();
    }
}
