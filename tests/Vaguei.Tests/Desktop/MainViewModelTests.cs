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
    public async Task ProcessResumeAsync_PopulatesProfileAndJobs()
    {
        var filePath = CreateTemporaryResume();

        try
        {
            var source = new StubJobSource();
            var viewModel = CreateViewModel(source);

            await viewModel.ProcessResumeAsync(filePath);

            Assert.True(viewModel.HasProfile);
            Assert.True(
                viewModel.HasResults,
                viewModel.StatusMessage);
            Assert.Single(viewModel.Jobs);
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
    public async Task RefreshJobsCommand_ReusesAnalyzedProfile()
    {
        var filePath = CreateTemporaryResume();

        try
        {
            var source = new StubJobSource();
            var viewModel = CreateViewModel(source);

            await viewModel.ProcessResumeAsync(filePath);
            await viewModel.RefreshJobsCommand.ExecuteAsync(null);

            Assert.Equal(2, source.SearchCount);
            Assert.True(
                viewModel.RefreshJobsCommand.CanExecute(null));
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

            IEnumerable<JobPosting> jobs =
            [
                new JobPosting
                {
                    Title = "Analista de Dados",
                    Company = "Empresa Teste",
                    Description = "Consultas SQL.",
                    Source = Name,
                    PublishedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
                    Location = new JobLocation
                    {
                        RawLocation = "Brasil"
                    }
                }
            ];

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
