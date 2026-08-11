using Avalonia;
using Avalonia.Headless;
using LifeBucketList.App.Tests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace LifeBucketList.App.Tests;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<LifeBucketList.App.App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
