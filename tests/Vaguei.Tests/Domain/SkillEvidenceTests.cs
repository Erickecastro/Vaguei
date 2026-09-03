using Vaguei.Domain.Enums;
using Vaguei.Domain.Models;

namespace Vaguei.Tests.Domain;

public sealed class SkillEvidenceTests
{
    [Fact]
    public void Constructor_PreservesEvidenceSource()
    {
        var evidence =
            new SkillEvidence(
                SkillEvidenceSource.Project);

        Assert.Equal(
            SkillEvidenceSource.Project,
            evidence.Source);
    }
}
