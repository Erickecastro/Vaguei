using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vaguei.Application.Services;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Mobile.ViewModels;

public sealed class MobileMainViewModel : ObservableObject
{
    private readonly JobSearchOrchestrator _orchestrator;
    private string _query = string.Empty;
    private string _status = "Digite um cargo, tecnologia ou empresa para começar.";
    private bool _isBusy;

    public MobileMainViewModel(JobSearchOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
        SearchCommand = new AsyncRelayCommand(
            SearchAsync,
            () => !IsBusy && !string.IsNullOrWhiteSpace(Query));
    }

    public ObservableCollection<MobileJobItem> Jobs { get; } = [];

    public IAsyncRelayCommand SearchCommand { get; }

    public string Query
    {
        get => _query;
        set
        {
            if (SetProperty(ref _query, value)) SearchCommand.NotifyCanExecuteChanged();
        }
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value)) SearchCommand.NotifyCanExecuteChanged();
        }
    }

    private async Task SearchAsync()
    {
        IsBusy = true;
        Status = "Procurando oportunidades...";

        try
        {
            var preferences = new JobSearchPreferences
            {
                IncludeBrazil = true,
                IncludeInternational = true,
                PublicationWindow = JobPublicationWindow.Last3Months
            };
            preferences.DesiredRoles.Add(Query.Trim());

            var result = await _orchestrator.SearchAsync(
                new CandidateProfile(),
                preferences,
                DateTimeOffset.UtcNow);

            Jobs.Clear();
            foreach (var match in result.Matches.Take(100))
            {
                Jobs.Add(new MobileJobItem(
                    match.Job.Title,
                    match.Job.Company,
                    match.Job.Location.RawLocation ?? "Local não informado",
                    match.Job.Source ?? "Fonte não informada",
                    match.Job.Url?.AbsoluteUri));
            }

            Status = Jobs.Count == 0
                ? "Nenhuma vaga encontrada. Tente outro termo."
                : $"{Jobs.Count} oportunidades encontradas.";
        }
        catch (Exception)
        {
            Status = "Não foi possível concluir a busca. Verifique sua conexão.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public sealed record MobileJobItem(
    string Title,
    string Company,
    string Location,
    string Source,
    string? Url);
