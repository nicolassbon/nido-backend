namespace Nido.Application.Common.ProfileImages;

public interface IProfileImagePublicUrlResolver
{
    string? Resolve(string? storageKey);
}
