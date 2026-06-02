using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Nido.Api.OpenApi;

public class SecurityRequirementsOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (operation is null || context is null)
        {
            return Task.CompletedTask;
        }

        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        
        var hasAllowAnonymous = metadata.OfType<AllowAnonymousAttribute>().Any();
        var hasAuthorize = metadata.OfType<AuthorizeAttribute>().Any();

        if (hasAuthorize && !hasAllowAnonymous)
        {
            operation.Security ??= new List<OpenApiSecurityRequirement>();
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = new List<string>()
            });
        }
        
        return Task.CompletedTask;
    }
}
