using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Vaguei.Application.Services;
using Vaguei.Collectors.Configuration;
using Vaguei.Mobile.ViewModels;
using Vaguei.Mobile.Views;

namespace Vaguei.Mobile;

public partial class App : Avalonia.Application
{
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IActivityApplicationLifetime activity)
        {
            activity.MainViewFactory = CreateMainView;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = CreateMainView();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private MainView CreateMainView() => new()
    {
        DataContext = new MobileMainViewModel(
            new JobSearchOrchestrator(JobSourceFactory.Create(_httpClient)))
    };
}
