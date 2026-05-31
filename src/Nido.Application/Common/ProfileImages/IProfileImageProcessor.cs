using Nido.Application.Auth.Register;

namespace Nido.Application.Common.ProfileImages;

public interface IProfileImageProcessor
{
    Task<ProcessedProfileImage> ProcessAsync(RegistrationProfileImageUpload upload, CancellationToken cancellationToken);
}
