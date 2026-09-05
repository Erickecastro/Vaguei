namespace Vaguei.Application.Interfaces;

public interface IThemePreferenceStore
{
    string? Load();

    void Save(string theme);
}
