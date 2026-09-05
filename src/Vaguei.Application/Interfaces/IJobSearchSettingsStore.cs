using Vaguei.Application.Models;

namespace Vaguei.Application.Interfaces;

public interface IJobSearchSettingsStore
{
    JobSearchSettings Load();

    void Save(JobSearchSettings settings);
}
