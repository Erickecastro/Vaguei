using Vaguei.Application.Services;
using Vaguei.Domain.Enums;

namespace Vaguei.Tests.Application;

public sealed class ResumeSectionClassifierTests
{
    private readonly ResumeSectionClassifier _classifier = new();

    [Fact]
    public void Classify_RecognizesPortugueseSectionsWithAccents()
    {
        var resume = string.Join(
            Environment.NewLine,
            "COMPETÊNCIAS:",
            "Comunicação e liderança",
            "CERTIFICAÇÕES",
            "Gestão de projetos");

        var sections = _classifier.Classify(resume);

        Assert.Equal(
            "Comunicação e liderança",
            sections[SkillEvidenceSource.SkillsSection]);

        Assert.Equal(
            "Gestão de projetos",
            sections[SkillEvidenceSource.Certification]);
    }

    [Fact]
    public void Classify_RecognizesEnglishSections()
    {
        var resume = string.Join(
            Environment.NewLine,
            "SKILLS",
            "Financial analysis",
            "PROJECTS",
            "Annual planning");

        var sections = _classifier.Classify(resume);

        Assert.Contains(
            SkillEvidenceSource.SkillsSection,
            sections.Keys);

        Assert.Contains(
            SkillEvidenceSource.Project,
            sections.Keys);
    }

    [Fact]
    public void Classify_IgnoresTextOutsideKnownSections()
    {
        var sections = _classifier.Classify(
            "Unstructured resume text");

        Assert.Empty(sections);
    }
}
