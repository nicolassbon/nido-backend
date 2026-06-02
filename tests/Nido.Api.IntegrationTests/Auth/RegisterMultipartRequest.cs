using System.Net.Http.Headers;

namespace Nido.Api.IntegrationTests.Auth;

internal static class RegisterMultipartRequest
{
    public static MultipartFormDataContent Create(
        string nombre,
        string email,
        string password,
        string sexo,
        byte[]? foto = null,
        string fileName = "avatar.png",
        string contentType = "image/png")
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(nombre), "nombre" },
            { new StringContent(email), "email" },
            { new StringContent(password), "password" },
            { new StringContent(sexo), "sexo" }
        };

        if (foto is not null)
        {
            var fileContent = new ByteArrayContent(foto);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            content.Add(fileContent, "foto", fileName);
        }

        return content;
    }
}
