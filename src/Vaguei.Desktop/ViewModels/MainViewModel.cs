using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vaguei.Application.Services;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;
using Vaguei.ResumeParser.Services;

namespace Vaguei.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ResumeParserService _parserService;
    private readonly ResumeAnalyzer _resumeAnalyzer;
    private readonly JobSearchOrchestrator _searchOrchestrator;
    private CandidateProfile? _currentProfile;

    [ObservableProperty]
    private string _selectedFileName = "Nenhum currículo selecionado";

    [ObservableProperty]
    private string _statusMessage =
        "Arraste seu currículo para começar ou escolha um arquivo.";

    [ObservableProperty]
    private string _profileSummary = string.Empty;

    [ObservableProperty]
    private string _candidateName = string.Empty;

    [ObservableProperty]
    private string _professionalTitle = string.Empty;

    [ObservableProperty]
    private int _additionalSkillCount;

    [ObservableProperty]
    private string _sourceWarnings = string.Empty;

    [ObservableProperty]
    private bool _includeInternational;

    [ObservableProperty]
    private int _searchScopeIndex;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshJobsCommand))]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    private bool _isBusy;

    [ObservableProperty]
    private bool _hasProfile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    private bool _hasResults;

    public MainViewModel(
        ResumeParserService parserService,
        ResumeAnalyzer resumeAnalyzer,
        JobSearchOrchestrator searchOrchestrator)
    {
        _parserService = parserService;
        _resumeAnalyzer = resumeAnalyzer;
        _searchOrchestrator = searchOrchestrator;
    }

    public ObservableCollection<JobResultItemViewModel> Jobs { get; } = [];

    public ObservableCollection<string> ProfileSkills { get; } = [];

    public IReadOnlyList<string> SearchScopes { get; } =
    [
        "Somente Brasil",
        "Brasil + exterior"
    ];

    public bool ShowEmptyState => !IsBusy && !HasResults;

    public async Task ProcessResumeAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        HasResults = false;
        SourceWarnings = string.Empty;
        Jobs.Clear();
        SelectedFileName = Path.GetFileName(filePath);
        StatusMessage = "Analisando currículo...";

        try
        {
            var extension = Path.GetExtension(filePath);
            var parser = _parserService.GetParser(extension);

            var text = await Task.Run(
                async () =>
                {
                    await using var stream = File.OpenRead(filePath);

                    return await parser.ExtractTextAsync(
                            stream,
                            cancellationToken)
                        .ConfigureAwait(false);
                },
                cancellationToken);

            var profile = await Task.Run(
                () => _resumeAnalyzer.Analyze(text),
                cancellationToken);
            _currentProfile = profile;
            CandidateName = profile.Name;
            ProfessionalTitle = profile.ProfessionalTitle;
            PopulateProfileSkills(profile);
            ProfileSummary = CreateProfileSummary(profile);
            HasProfile = true;
            RefreshJobsCommand.NotifyCanExecuteChanged();

            await SearchJobsAsync(
                profile,
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            StatusMessage = "Processamento cancelado.";
        }
        catch (Exception exception)
        {
            HasProfile = false;
            HasResults = false;
            StatusMessage = $"Não foi possível processar o currículo: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRefreshJobs))]
    private async Task RefreshJobsAsync()
    {
        if (_currentProfile is null)
        {
            return;
        }

        IsBusy = true;
        HasResults = false;
        Jobs.Clear();

        try
        {
            await SearchJobsAsync(
                _currentProfile,
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            StatusMessage = $"Não foi possível buscar novas vagas: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanRefreshJobs()
    {
        return _currentProfile is not null &&
               !IsBusy;
    }

    partial void OnSearchScopeIndexChanged(
        int value)
    {
        IncludeInternational = value == 1;
    }

    private async Task SearchJobsAsync(
        CandidateProfile profile,
        CancellationToken cancellationToken)
    {
        StatusMessage = "Buscando novas oportunidades...";
        SourceWarnings = string.Empty;

        var preferences = new JobSearchPreferences
        {
            IncludeBrazil = true,
            IncludeInternational = IncludeInternational
        };

        var result = await Task.Run(
            async () => await _searchOrchestrator.SearchAsync(
                    profile,
                    preferences,
                    DateTimeOffset.UtcNow,
                    cancellationToken)
                .ConfigureAwait(false),
            cancellationToken);

        Jobs.Clear();

        foreach (var match in result.Matches.Take(50))
        {
            Jobs.Add(
                new JobResultItemViewModel(match));
        }

        HasResults = Jobs.Count > 0;
        SourceWarnings = string.Join(
            Environment.NewLine,
            result.SourceFailures.Select(failure =>
                $"{failure.Source}: {failure.Message}"));

        StatusMessage = Jobs.Count == 0
            ? "Nenhuma vaga foi encontrada pelas fontes atuais com este filtro."
            : $"{Jobs.Count} oportunidades encontradas e ordenadas por compatibilidade.";
    }

    private void PopulateProfileSkills(
        CandidateProfile profile)
    {
        ProfileSkills.Clear();

        var skills = profile.DetailedSkills
            .OrderByDescending(skill => skill.Relevance)
            .ThenBy(skill => skill.Name)
            .ToArray();

        foreach (var skill in skills.Take(8))
        {
            ProfileSkills.Add(skill.Name);
        }

        AdditionalSkillCount = Math.Max(
            0,
            skills.Length - ProfileSkills.Count);
    }

    private static string CreateProfileSummary(
        CandidateProfile profile)
    {
        var primarySkills = profile.DetailedSkills
            .Where(skill =>
                skill.Relevance ==
                SkillRelevance.Primary)
            .Select(skill => skill.Name)
            .Take(8);

        var skills = string.Join(", ", primarySkills);

        return string.IsNullOrWhiteSpace(skills)
            ? $"{profile.Name} · {profile.ProfessionalTitle}"
            : $"{profile.Name} · {profile.ProfessionalTitle}{Environment.NewLine}{skills}";
    }
}
