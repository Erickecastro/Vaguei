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
    private string _detectedProfessionalTitle = string.Empty;

    [ObservableProperty]
    private string _selectedFileName = "Nenhum currículo";

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
    [NotifyCanExecuteChangedFor(nameof(RefreshJobsCommand))]
    private string _desiredRole = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAdditionalSkills))]
    private int _additionalSkillCount;

    [ObservableProperty]
    private string _sourceWarnings = string.Empty;

    [ObservableProperty]
    private bool _includeInternational;

    [ObservableProperty]
    private int _searchScopeIndex;

    [ObservableProperty]
    private int _publicationWindowIndex = 3;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshJobsCommand))]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResultsSubtitle))]
    private bool _hasProfile;

    [ObservableProperty]
    private bool _isSearchAttentionActive;

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

    public IReadOnlyList<string> PublicationWindows { get; } =
    [
        "Últimas 24 horas",
        "Últimos 3 dias",
        "Últimos 7 dias",
        "Últimos 30 dias",
        "Últimos 3 meses"
    ];

    public bool ShowEmptyState => !IsBusy && !HasResults;

    public bool HasAdditionalSkills => AdditionalSkillCount > 0;

    public string ResultsSubtitle => HasProfile
        ? "Ordenadas por compatibilidade e recência"
        : "Resultados da pesquisa direta ordenados por recência";

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
        HasProfile = false;
        IsSearchAttentionActive = false;
        _currentProfile = null;
        RefreshJobsCommand.NotifyCanExecuteChanged();
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
            _detectedProfessionalTitle = profile.ProfessionalTitle;
            DesiredRole = profile.ProfessionalTitle;
            PopulateProfileSkills(profile);
            ProfileSummary = CreateProfileSummary(profile);
            HasProfile = true;
            IsSearchAttentionActive = true;
            RefreshJobsCommand.NotifyCanExecuteChanged();
            StatusMessage =
                "Currículo analisado. Clique em Pesquisar para encontrar vagas.";
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
        var profile = _currentProfile ?? new CandidateProfile();

        IsSearchAttentionActive = false;
        IsBusy = true;
        HasResults = false;
        Jobs.Clear();

        try
        {
            await SearchJobsAsync(
                profile,
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
        return (_currentProfile is not null || IsValidDirectSearch(DesiredRole)) &&
               !IsBusy;
    }

    private static bool IsValidDirectSearch(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();

        return normalized.Length is >= 2 and <= 80 &&
               normalized.Any(char.IsLetterOrDigit);
    }

    [RelayCommand]
    private void RemoveResume()
    {
        if (IsBusy)
        {
            return;
        }

        _currentProfile = null;
        SelectedFileName = "Nenhum currículo";
        CandidateName = string.Empty;
        ProfessionalTitle = string.Empty;
        _detectedProfessionalTitle = string.Empty;
        DesiredRole = string.Empty;
        ProfileSummary = string.Empty;
        AdditionalSkillCount = 0;
        ProfileSkills.Clear();
        Jobs.Clear();
        SourceWarnings = string.Empty;
        HasProfile = false;
        HasResults = false;
        IsSearchAttentionActive = false;
        StatusMessage =
            "Arraste seu currículo para começar ou escolha um arquivo.";
        RefreshJobsCommand.NotifyCanExecuteChanged();
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
            IncludeInternational = IncludeInternational,
            PublicationWindow = GetPublicationWindow()
        };

        var directSearch = DesiredRole.Trim();

        if (IsValidDirectSearch(directSearch) &&
            (_currentProfile is null ||
             !directSearch.Equals(
                _detectedProfessionalTitle.Trim(),
                StringComparison.OrdinalIgnoreCase)))
        {
            preferences.DesiredRoles.Add(directSearch);
        }

        var result = await Task.Run(
            async () => await _searchOrchestrator.SearchAsync(
                    profile,
                    preferences,
                    DateTimeOffset.UtcNow,
                    cancellationToken)
                .ConfigureAwait(false),
            cancellationToken);

        Jobs.Clear();

        foreach (var match in result.Matches)
        {
            Jobs.Add(
                new JobResultItemViewModel(
                    match,
                    showCompatibility: _currentProfile is not null));
        }

        HasResults = Jobs.Count > 0;
        SourceWarnings = string.Join(
            Environment.NewLine,
            result.SourceFailures.Select(failure =>
                $"{failure.Source}: {failure.Message}"));

        StatusMessage = Jobs.Count == 0
            ? IsValidDirectSearch(directSearch)
                ? $"Nenhum resultado relacionado a “{directSearch}”. Tente outro cargo, tecnologia ou empresa."
                : "Nenhuma vaga foi encontrada pelas fontes atuais com este filtro."
            : _currentProfile is null
                ? $"{Jobs.Count} oportunidades encontradas para “{directSearch}”."
                : $"{Jobs.Count} oportunidades encontradas e ordenadas por compatibilidade.";
    }

    private JobPublicationWindow GetPublicationWindow()
    {
        return PublicationWindowIndex switch
        {
            0 => JobPublicationWindow.Last24Hours,
            1 => JobPublicationWindow.Last3Days,
            2 => JobPublicationWindow.Last7Days,
            3 => JobPublicationWindow.Last30Days,
            4 => JobPublicationWindow.Last3Months,
            _ => JobPublicationWindow.Last30Days
        };
    }

    private void PopulateProfileSkills(
        CandidateProfile profile)
    {
        ProfileSkills.Clear();

        var skills = profile.DetailedSkills
            .OrderByDescending(skill => skill.Relevance)
            .ThenBy(skill => skill.Name)
            .ToArray();

        foreach (var skill in skills.Take(6))
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
