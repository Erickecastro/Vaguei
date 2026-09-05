using Vaguei.Application.Interfaces;
using Vaguei.Application.Services;
using Vaguei.Desktop.ViewModels;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Models;
using Vaguei.ResumeParser.Services;

namespace Vaguei.Tests.Desktop;

public sealed class MainViewModelTests
{
    [Fact]
    public void InitialState_ExposesSearchScopesAndEmptyState()
    {
        var viewModel = CreateViewModel(
            new StubJobSource());

        Assert.Equal(
            ["Somente Brasil", "Brasil + exterior"],
            viewModel.SearchScopes);
        Assert.Equal(
            [
                "Últimas 24 horas",
                "Últimos 3 dias",
                "Últimos 7 dias",
                "Últimos 30 dias",
                "Últimos 3 meses"
            ],
            viewModel.PublicationWindows);
        Assert.Equal(3, viewModel.PublicationWindowIndex);
        Assert.True(viewModel.ShowEmptyState);
    }

    [Fact]
    public async Task ProcessResumeAsync_PopulatesProfileAndWaitsForSearch()
    {
        var filePath = CreateTemporaryResume();

        try
        {
            var source = new StubJobSource();
            var viewModel = CreateViewModel(source);

            await viewModel.ProcessResumeAsync(filePath);

            Assert.True(viewModel.HasProfile);
            Assert.False(viewModel.HasResults);
            Assert.Empty(viewModel.Jobs);
            Assert.Equal(0, source.SearchCount);
            Assert.True(viewModel.IsSearchAttentionActive);
            Assert.Contains(
                "Pessoa Teste",
                viewModel.ProfileSummary);
            Assert.False(viewModel.IsBusy);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ProcessResumeAsync_UsesInternationalPreference()
    {
        var filePath = CreateTemporaryResume();

        try
        {
            var source = new StubJobSource();
            var viewModel = CreateViewModel(source);
            viewModel.SearchScopeIndex = 1;

            await viewModel.ProcessResumeAsync(filePath);
            await viewModel.RefreshJobsCommand.ExecuteAsync(null);

            Assert.NotNull(source.LastQuery);
            Assert.Empty(source.LastQuery.Locations);
            Assert.True(viewModel.IncludeInternational);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ProcessResumeAsync_ReportsUnsupportedFormat()
    {
        var filePath = Path.Combine(
            Path.GetTempPath(),
            $"vaguei-{Guid.NewGuid():N}.invalid");

        await File.WriteAllTextAsync(
            filePath,
            "Conteúdo de teste");

        try
        {
            var viewModel = CreateViewModel(
                new StubJobSource());

            await viewModel.ProcessResumeAsync(filePath);

            Assert.False(viewModel.HasProfile);
            Assert.False(viewModel.HasResults);
            Assert.Contains(
                "Formato de currículo não suportado",
                viewModel.StatusMessage);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ProcessResumeAsync_AppliesSelectedPublicationWindow()
    {
        var filePath = CreateTemporaryResume();

        try
        {
            var source = new StubJobSource
            {
                PublishedAt = DateTimeOffset.UtcNow.AddDays(-2)
            };

            var viewModel = CreateViewModel(source);
            viewModel.PublicationWindowIndex = 0;

            await viewModel.ProcessResumeAsync(filePath);
            await viewModel.RefreshJobsCommand.ExecuteAsync(null);

            Assert.False(viewModel.HasResults);
            Assert.Empty(viewModel.Jobs);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task RefreshJobsCommand_ReusesAnalyzedProfile()
    {
        var filePath = CreateTemporaryResume();

        try
        {
            var source = new StubJobSource();
            var viewModel = CreateViewModel(source);

            await viewModel.ProcessResumeAsync(filePath);
            await viewModel.RefreshJobsCommand.ExecuteAsync(null);

            Assert.Equal(1, source.SearchCount);
            Assert.NotNull(source.LastQuery);
            Assert.Contains("SQL", source.LastQuery.Keywords);
            Assert.False(viewModel.IsSearchAttentionActive);
            Assert.True(
                viewModel.RefreshJobsCommand.CanExecute(null));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task RefreshJobsCommand_UsesEditedDesiredRole()
    {
        var filePath = CreateTemporaryResume();

        try
        {
            var source = new StubJobSource();
            var viewModel = CreateViewModel(source);
            await viewModel.ProcessResumeAsync(filePath);

            viewModel.DesiredRole = "Especialista financeiro";
            await viewModel.RefreshJobsCommand.ExecuteAsync(null);

            Assert.NotNull(source.LastQuery);
            Assert.Contains("Especialista financeiro", source.LastQuery.Keywords);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task RefreshJobsCommand_DoesNotLimitResultsToFifty()
    {
        var filePath = CreateTemporaryResume();

        try
        {
            var source = new StubJobSource { ResultCount = 75 };
            var viewModel = CreateViewModel(source);
            await viewModel.ProcessResumeAsync(filePath);

            await viewModel.RefreshJobsCommand.ExecuteAsync(null);

            Assert.Equal(75, viewModel.Jobs.Count);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task RefreshJobsCommand_IsDisabledWhileSearchIsRunning()
    {
        var filePath = CreateTemporaryResume();

        try
        {
            var source = new StubJobSource();
            var viewModel = CreateViewModel(source);

            await viewModel.ProcessResumeAsync(filePath);
            source.BlockNextSearch();

            var refreshTask =
                viewModel.RefreshJobsCommand.ExecuteAsync(null);

            Assert.True(viewModel.IsBusy);
            Assert.False(viewModel.ShowEmptyState);
            Assert.False(
                viewModel.RefreshJobsCommand.CanExecute(null));

            source.ReleaseSearch();
            await refreshTask;

            Assert.False(viewModel.IsBusy);
            Assert.True(
                viewModel.RefreshJobsCommand.CanExecute(null));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task RemoveResumeCommand_ClearsProfileAndResults()
    {
        var filePath = CreateTemporaryResume();

        try
        {
            var viewModel = CreateViewModel(new StubJobSource());
            await viewModel.ProcessResumeAsync(filePath);
            await viewModel.RefreshJobsCommand.ExecuteAsync(null);

            viewModel.RemoveResumeCommand.Execute(null);

            Assert.False(viewModel.HasProfile);
            Assert.False(viewModel.HasResults);
            Assert.False(viewModel.IsSearchAttentionActive);
            Assert.Empty(viewModel.Jobs);
            Assert.Empty(viewModel.ProfileSkills);
            Assert.Empty(viewModel.DesiredRole);
            Assert.Equal("Nenhum currículo", viewModel.SelectedFileName);
            Assert.False(viewModel.RefreshJobsCommand.CanExecute(null));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static MainViewModel CreateViewModel(
        StubJobSource source)
    {
        return new MainViewModel(
            new ResumeParserService(
            [
                new StubResumeParser()
            ]),
            new ResumeAnalyzer(),
            new JobSearchOrchestrator([source]));
    }

    private static string CreateTemporaryResume()
    {
        var filePath = Path.Combine(
            Path.GetTempPath(),
            $"vaguei-{Guid.NewGuid():N}.txt");

        File.WriteAllText(
            filePath,
            "Currículo simulado");

        return filePath;
    }

    private sealed class StubResumeParser : IResumeParser
    {
        public bool CanParse(string extension)
        {
            return extension.Equals(
                ".txt",
                StringComparison.OrdinalIgnoreCase);
        }

        public Task<string> ExtractTextAsync(
            Stream fileStream,
            CancellationToken cancellationToken = default)
        {
            const string resume =
                """
                Pessoa Teste
                Analista de Dados

                LINGUAGENS E TECNOLOGIAS
                SQL
                """;

            return Task.FromResult(resume);
        }
    }

    private sealed class StubJobSource : IJobSource
    {
        private TaskCompletionSource? _searchGate;

        public string Name => "Fonte simulada";

        public JobSearchQuery? LastQuery { get; private set; }

        public int SearchCount { get; private set; }

        public DateTimeOffset PublishedAt { get; init; } =
            DateTimeOffset.UtcNow.AddMinutes(-1);

        public int ResultCount { get; init; } = 1;

        public async Task<IEnumerable<JobPosting>> SearchAsync(
            JobSearchQuery query,
            CancellationToken cancellationToken = default)
        {
            LastQuery = query;
            SearchCount++;

            if (_searchGate is not null)
            {
                await _searchGate.Task.WaitAsync(
                    cancellationToken);
            }

            var jobs = Enumerable.Range(1, ResultCount)
                .Select(index =>
                new JobPosting
                {
                    Title = $"Analista de Dados {index}",
                    Company = "Empresa Teste",
                    Description = "Consultas SQL.",
                    Source = Name,
                    PublishedAt = PublishedAt,
                    Location = new JobLocation
                    {
                        RawLocation = "Brasil"
                    }
                });

            return jobs;
        }

        public void BlockNextSearch()
        {
            _searchGate = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public void ReleaseSearch()
        {
            _searchGate?.SetResult();
            _searchGate = null;
        }
    }
}
