namespace Vaguei.Application.Interfaces;

public interface IFavoriteJobStore
{
    IReadOnlySet<string> Load();

    void Save(IReadOnlySet<string> favoriteKeys);
}
