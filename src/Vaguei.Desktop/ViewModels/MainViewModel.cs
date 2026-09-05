using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vaguei.Application.Models;
using Vaguei.Application.Services;
using Vaguei.Application.Interfaces;
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
    private readonly IFavoriteJobStore? _favoriteStore;
    private readonly IJobSearchSettingsStore? _searchSettingsStore;
    private readonly TimeSpan _searchTimeout;
    private readonly Func<bool>? _networkAvailable;
    private readonly bool _showDetailedSourceWarnings;
    private readonly HashSet<string> _favoriteKeys;
    private readonly List<JobResultItemViewModel> _allJobs = [];
    private bool _searchSettingsLoaded;
    private CancellationTokenSource? _settingsSaveCancellation;
    private CandidateProfile? _currentProfile;
    private string _detectedProfessionalTitle = string.Empty;
    private CancellationTokenSource? _connectionNoticeCancellation;
    private CancellationTokenSource? _activeSearchCancellation;
    private bool _connectionLostDuringSearch;

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
    private string _sourceCoverageSummary = string.Empty;

    [ObservableProperty]
    private bool _includeInternational;

    [ObservableProperty]
    private int _searchScopeIndex;

    [ObservableProperty]
    private int _publicationWindowIndex = 3;

    [ObservableProperty]
    private int _workModelIndex;

    [ObservableProperty]
    private int _employmentTypeIndex;

    [ObservableProperty]
    private int _seniorityIndex;

    [ObservableProperty]
    private string _locationFilter = string.Empty;

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

    [ObservableProperty]
    private bool _isConnectionNoticeVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FavoritesFilterLabel))]
    [NotifyPropertyChangedFor(nameof(EmptyStateTitle))]
    [NotifyPropertyChangedFor(nameof(EmptyStateDescription))]
    private bool _showOnlyFavorites;

    [ObservableProperty]
    private string _connectionNoticeMessage =
        "Sem conexão com a internet. Verifique sua rede e tente novamente.";

    public MainViewModel(
        ResumeParserService parserService,
        ResumeAnalyzer resumeAnalyzer,
        JobSearchOrchestrator searchOrchestrator,
        IFavoriteJobStore? favoriteStore = null,
        IJobSearchSettingsStore? searchSettingsStore = null,
        TimeSpan? searchTimeout = null,
        Func<bool>? networkAvailable = null,
        bool showDetailedSourceWarnings = true)
    {
        _parserService = parserService;
        _resumeAnalyzer = resumeAnalyzer;
        _searchOrchestrator = searchOrchestrator;
        _favoriteStore = favoriteStore;
        _searchSettingsStore = searchSettingsStore;
        _searchTimeout = searchTimeout ?? TimeSpan.FromMinutes(2);
        _networkAvailable = networkAvailable;
        _showDetailedSourceWarnings = showDetailedSourceWarnings;
        _favoriteKeys = favoriteStore?.Load().ToHashSet(StringComparer.OrdinalIgnoreCase) ??
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var settings = searchSettingsStore?.Load() ?? new JobSearchSettings();
        SearchScopeIndex = Math.Clamp(settings.SearchScopeIndex, 0, SearchScopes.Count - 1);
        PublicationWindowIndex = Math.Clamp(settings.PublicationWindowIndex, 0, PublicationWindows.Count - 1);
        WorkModelIndex = Math.Clamp(settings.WorkModelIndex, 0, WorkModelOptions.Count - 1);
        EmploymentTypeIndex = Math.Clamp(settings.EmploymentTypeIndex, 0, EmploymentTypeOptions.Count - 1);
        SeniorityIndex = Math.Clamp(settings.SeniorityIndex, 0, SeniorityOptions.Count - 1);
        LocationFilter = settings.LocationFilter?.Trim() ?? string.Empty;
        _searchSettingsLoaded = true;
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

    public IReadOnlyList<string> WorkModelOptions { get; } =
    [
        "Qualquer modelo",
        "Remoto",
        "Híbrido",
        "Presencial"
    ];

    public IReadOnlyList<string> EmploymentTypeOptions { get; } =
    [
        "Qualquer contrato",
        "Tempo integral",
        "Meio período",
        "Contrato",
        "Temporário",
        "Freelance",
        "Estágio"
    ];

    public IReadOnlyList<string> SeniorityOptions { get; } =
    [
        "Qualquer senioridade",
        "Estágio",
        "Trainee",
        "Júnior",
        "Pleno",
        "Sênior",
        "Liderança"
    ];

    public bool ShowEmptyState => !IsBusy && !HasResults;

    public bool HasAdditionalSkills => AdditionalSkillCount > 0;

    public bool HasActiveFilters =>
        PublicationWindowIndex != 3 ||
        WorkModelIndex != 0 ||
        EmploymentTypeIndex != 0 ||
        SeniorityIndex != 0 ||
        !string.IsNullOrWhiteSpace(LocationFilter) ||
        ShowOnlyFavorites;

    public string FavoritesFilterLabel => ShowOnlyFavorites
        ? "Mostrar todas"
        : "Somente salvas";

    public string EmptyStateTitle => ShowOnlyFavorites
        ? "Nenhuma vaga salva nesta pesquisa"
        : "Suas vagas aparecerão aqui";

    public string EmptyStateDescription => ShowOnlyFavorites
        ? "Marque a estrela de uma oportunidade para encontrá-la aqui."
        : "Envie seu currículo ou pesquise diretamente por cargo, tecnologia ou empresa.";

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
        _allJobs.Clear();
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
        DismissConnectionNotice();

        if (_networkAvailable?.Invoke() == false)
        {
            StatusMessage = "Sem conexão. Conecte-se à internet e tente novamente.";
            ShowConnectionNotice();
            return;
        }

        var profile = _currentProfile ?? new CandidateProfile();
        using var timeoutSource = new CancellationTokenSource(_searchTimeout);
        _activeSearchCancellation = timeoutSource;
        _connectionLostDuringSearch = false;

        IsSearchAttentionActive = false;
        IsBusy = true;

        try
        {
            var searchTask = SearchJobsAsync(
                profile,
                timeoutSource.Token);
            var completedTask = await Task.WhenAny(
                searchTask,
                Task.Delay(_searchTimeout, timeoutSource.Token));

            if (completedTask != searchTask)
            {
                timeoutSource.Cancel();
                _ = ObserveAbandonedSearchAsync(searchTask);
                StatusMessage = _connectionLostDuringSearch
                    ? "Busca interrompida: conexão perdida."
                    : "A busca demorou mais que o esperado. Tente novamente.";
                ShowConnectionNotice();
                return;
            }

            await searchTask;
        }
        catch (OperationCanceledException)
            when (timeoutSource.IsCancellationRequested)
        {
            StatusMessage = _connectionLostDuringSearch
                ? "Busca interrompida: conexão perdida."
                : "A busca demorou mais que o esperado. Tente novamente.";
            ShowConnectionNotice();
        }
        catch (Exception exception)
        {
            StatusMessage = $"Não foi possível buscar novas vagas: {exception.Message}";
        }
        finally
        {
            if (ReferenceEquals(_activeSearchCancellation, timeoutSource))
            {
                _activeSearchCancellation = null;
            }
            IsBusy = false;
        }
    }

    public void HandleNetworkAvailabilityChanged(bool isAvailable)
    {
        if (isAvailable) return;

        ConnectionNoticeMessage = "Sem conexão com a internet. Verifique sua rede e tente novamente.";
        _connectionLostDuringSearch = IsBusy;
        _activeSearchCancellation?.Cancel();

        if (IsBusy)
        {
            StatusMessage = "Busca interrompida: conexão perdida.";
        }

        ShowConnectionNotice();
    }

    private static async Task ObserveAbandonedSearchAsync(Task searchTask)
    {
        try
        {
            await searchTask.ConfigureAwait(false);
        }
        catch
        {
            // A interface já informou o timeout; apenas observa a tarefa abandonada.
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
        _allJobs.Clear();
        SourceWarnings = string.Empty;
        HasProfile = false;
        HasResults = false;
        IsSearchAttentionActive = false;
        StatusMessage =
            "Arraste seu currículo para começar ou escolha um arquivo.";
        RefreshJobsCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void ClearAdvancedFilters()
    {
        PublicationWindowIndex = 3;
        WorkModelIndex = 0;
        EmploymentTypeIndex = 0;
        SeniorityIndex = 0;
        LocationFilter = string.Empty;
        ShowOnlyFavorites = false;
        ApplyVisibleJobFilter();
    }

    partial void OnSearchScopeIndexChanged(
        int value)
    {
        IncludeInternational = value == 1;
        PersistSearchSettings();
    }

    partial void OnPublicationWindowIndexChanged(int value) => OnFilterChanged();
    partial void OnWorkModelIndexChanged(int value) => OnFilterChanged();
    partial void OnEmploymentTypeIndexChanged(int value) => OnFilterChanged();
    partial void OnSeniorityIndexChanged(int value) => OnFilterChanged();
    partial void OnLocationFilterChanged(string value) => OnFilterChanged();
    partial void OnShowOnlyFavoritesChanged(bool value) => OnPropertyChanged(nameof(HasActiveFilters));

    [RelayCommand]
    private void ToggleFavoritesFilter()
    {
        ShowOnlyFavorites = !ShowOnlyFavorites;
        ApplyVisibleJobFilter();
    }

    private async Task SearchJobsAsync(
        CandidateProfile profile,
        CancellationToken cancellationToken)
    {
        StatusMessage = "Buscando novas oportunidades...";
        SourceWarnings = string.Empty;
        SourceCoverageSummary = string.Empty;

        var preferences = new JobSearchPreferences
        {
            IncludeBrazil = true,
            IncludeInternational = IncludeInternational,
            PublicationWindow = GetPublicationWindow()
        };

        var selectedWorkModel = GetSelectedWorkModel();

        if (selectedWorkModel is not null)
        {
            preferences.WorkModels.Add(selectedWorkModel.Value);
        }

        var selectedEmploymentType = GetSelectedEmploymentType();

        if (selectedEmploymentType is not null)
        {
            preferences.EmploymentTypes.Add(selectedEmploymentType.Value);
        }

        var selectedSeniority = GetSelectedSeniority();

        if (selectedSeniority is not null)
        {
            preferences.SeniorityLevels.Add(selectedSeniority.Value);
        }

        if (!string.IsNullOrWhiteSpace(LocationFilter))
        {
            preferences.Cities.Add(LocationFilter.Trim());
        }

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

        cancellationToken.ThrowIfCancellationRequested();

        Jobs.Clear();
        _allJobs.Clear();

        foreach (var match in result.Matches)
        {
            _allJobs.Add(
                new JobResultItemViewModel(
                    match,
                    showCompatibility: _currentProfile is not null,
                    isFavorite: IsFavorite(match),
                    favoriteChanged: OnFavoriteChanged));
        }

        ApplyVisibleJobFilter();

        HasResults = Jobs.Count > 0;

        if (result.AllSourcesFailed)
        {
            ShowConnectionNotice();
        }

        SourceWarnings = result.AllSourcesFailed || result.SourceFailures.Count == 0
            ? string.Empty
            : _showDetailedSourceWarnings
                ? string.Join(
                    Environment.NewLine,
                    result.SourceFailures.Select(failure =>
                        $"{failure.Source}: {failure.Message}"))
                : $"{result.SourceFailures.Count} fontes ficaram indisponíveis nesta busca.";

        SourceCoverageSummary = CreateSourceCoverageSummary(
            result.SourceSummaries);

        StatusMessage = _allJobs.Count == 0
            ? IsValidDirectSearch(directSearch)
                ? $"Nenhum resultado relacionado a “{directSearch}”. Tente outro cargo, tecnologia ou empresa."
                : "Nenhuma vaga foi encontrada pelas fontes atuais com este filtro."
            : _currentProfile is null
                ? $"{_allJobs.Count} oportunidades encontradas para “{directSearch}” em {result.SourcesWithResults} fontes."
                : $"{_allJobs.Count} oportunidades encontradas em {result.SourcesWithResults} fontes e ordenadas por compatibilidade.";
    }

    [RelayCommand]
    private void DismissConnectionNotice()
    {
        _connectionNoticeCancellation?.Cancel();
        _connectionNoticeCancellation?.Dispose();
        _connectionNoticeCancellation = null;
        IsConnectionNoticeVisible = false;
    }

    private void ShowConnectionNotice()
    {
        DismissConnectionNotice();
        IsConnectionNoticeVisible = true;
        _connectionNoticeCancellation = new CancellationTokenSource();
        _ = AutoDismissConnectionNoticeAsync(_connectionNoticeCancellation.Token);
    }

    private async Task AutoDismissConnectionNoticeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(7), cancellationToken);
            IsConnectionNoticeVisible = false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private bool IsFavorite(JobMatchResult match)
    {
        var probe = new JobResultItemViewModel(match);
        return _favoriteKeys.Contains(probe.FavoriteKey);
    }

    private void OnFavoriteChanged(string key, bool isFavorite)
    {
        if (isFavorite) _favoriteKeys.Add(key); else _favoriteKeys.Remove(key);
        try { _favoriteStore?.Save(_favoriteKeys); }
        catch (IOException) { SourceWarnings = "Não foi possível salvar os favoritos localmente."; }
        catch (UnauthorizedAccessException) { SourceWarnings = "Não foi possível salvar os favoritos localmente."; }

        if (ShowOnlyFavorites)
        {
            ApplyVisibleJobFilter();
        }
    }

    private void ApplyVisibleJobFilter()
    {
        Jobs.Clear();
        foreach (var job in _allJobs.Where(job => !ShowOnlyFavorites || job.IsFavorite))
        {
            Jobs.Add(job);
        }

        HasResults = Jobs.Count > 0;
    }

    private void PersistSearchSettings()
    {
        if (!_searchSettingsLoaded || _searchSettingsStore is null)
        {
            return;
        }

        var settings = new JobSearchSettings
        {
            SearchScopeIndex = SearchScopeIndex,
            PublicationWindowIndex = PublicationWindowIndex,
            WorkModelIndex = WorkModelIndex,
            EmploymentTypeIndex = EmploymentTypeIndex,
            SeniorityIndex = SeniorityIndex,
            LocationFilter = LocationFilter
        };

        _settingsSaveCancellation?.Cancel();
        _settingsSaveCancellation?.Dispose();
        _settingsSaveCancellation = new CancellationTokenSource();
        _ = PersistSearchSettingsAsync(settings, _settingsSaveCancellation.Token);
    }

    private void OnFilterChanged()
    {
        OnPropertyChanged(nameof(HasActiveFilters));
        PersistSearchSettings();
    }

    private async Task PersistSearchSettingsAsync(
        JobSearchSettings settings,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(250, cancellationToken);
            await Task.Run(
                () => _searchSettingsStore!.Save(settings),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (IOException) { SourceWarnings = "Não foi possível salvar as preferências localmente."; }
        catch (UnauthorizedAccessException) { SourceWarnings = "Não foi possível salvar as preferências localmente."; }
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

    private static string CreateSourceCoverageSummary(
        IEnumerable<JobSourceSearchSummary> summaries)
    {
        var sources = summaries
            .Where(summary => summary.JobCount > 0)
            .OrderByDescending(summary => summary.JobCount)
            .ThenBy(summary => summary.Source)
            .Select(summary => $"{summary.Source} ({summary.JobCount})");
        var value = string.Join(" · ", sources);

        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : $"Fontes: {value}";
    }

    private WorkModel? GetSelectedWorkModel()
    {
        return WorkModelIndex switch
        {
            1 => WorkModel.Remote,
            2 => WorkModel.Hybrid,
            3 => WorkModel.OnSite,
            _ => null
        };
    }

    private EmploymentType? GetSelectedEmploymentType() => EmploymentTypeIndex switch
    {
        1 => EmploymentType.FullTime,
        2 => EmploymentType.PartTime,
        3 => EmploymentType.Contract,
        4 => EmploymentType.Temporary,
        5 => EmploymentType.Freelance,
        6 => EmploymentType.Internship,
        _ => null
    };

    private SeniorityLevel? GetSelectedSeniority() => SeniorityIndex switch
    {
        1 => SeniorityLevel.Internship,
        2 => SeniorityLevel.Trainee,
        3 => SeniorityLevel.Junior,
        4 => SeniorityLevel.MidLevel,
        5 => SeniorityLevel.Senior,
        6 => SeniorityLevel.Lead,
        _ => null
    };

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
