namespace Nido.Application.Common.Assets;

public interface IPublicAssetUrlResolver
{
    string? Resolve(string? value);
}
