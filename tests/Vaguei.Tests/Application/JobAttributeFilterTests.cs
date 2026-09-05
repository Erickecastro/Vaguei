using Vaguei.Application.Services;
using Vaguei.Domain.Entities;
using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Tests.Application;

public sealed class JobAttributeFilterTests
{
    private readonly JobAttributeFilter _filter = new();

    [Fact]
    public void Filter_AppliesWorkModelEmploymentTypeAndSeniorityTogether()
    {
        var matching = CreateJob(WorkModel.Remote, EmploymentType.FullTime, SeniorityLevel.Senior);
        var wrongContract = CreateJob(WorkModel.Remote, EmploymentType.Contract, SeniorityLevel.Senior);
        var wrongSeniority = CreateJob(WorkModel.Remote, EmploymentType.FullTime, SeniorityLevel.Junior);

        var preferences = new JobSearchPreferences
        {
            WorkModels = [WorkModel.Remote],
            EmploymentTypes = [EmploymentType.FullTime],
            SeniorityLevels = [SeniorityLevel.Senior]
        };

        var result = _filter.Filter(
            [matching, wrongContract, wrongSeniority],
            preferences);

        Assert.Same(matching, Assert.Single(result));
    }

    [Fact]
    public void Filter_WithNoAttributes_PreservesAllJobs()
    {
        var jobs = new[]
        {
            CreateJob(WorkModel.Unknown, EmploymentType.Unknown, SeniorityLevel.Unknown),
            CreateJob(WorkModel.Hybrid, EmploymentType.Internship, SeniorityLevel.Internship)
        };

        Assert.Equal(2, _filter.Filter(jobs, new JobSearchPreferences()).Count);
    }

    [Theory]
    [InlineData("Pessoa Desenvolvedora Júnior", SeniorityLevel.Junior)]
    [InlineData("Analista Pleno", SeniorityLevel.MidLevel)]
    [InlineData("Senior Software Engineer", SeniorityLevel.Senior)]
    [InlineData("Tech Lead", SeniorityLevel.Lead)]
    [InlineData("Estágio em Administração", SeniorityLevel.Internship)]
    public void Filter_InfersSeniorityFromJobContent(
        string title,
        SeniorityLevel expected)
    {
        var job = CreateJob(WorkModel.Remote, EmploymentType.FullTime, SeniorityLevel.Unknown);
        job.Title = title;

        var preferences = new JobSearchPreferences
        {
            SeniorityLevels = [expected]
        };

        Assert.Single(_filter.Filter([job], preferences));
    }

    private static JobPosting CreateJob(
        WorkModel workModel,
        EmploymentType employmentType,
        SeniorityLevel seniority) => new()
    {
        Title = "Vaga",
        Company = "Empresa",
        WorkModel = workModel,
        EmploymentType = employmentType,
        SeniorityLevel = seniority
    };
}
