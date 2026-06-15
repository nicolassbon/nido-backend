# Design: Spaces Images & Profile Avatars

## Technical Approach

Extend the existing controller → Application handler → Infrastructure repository flow without changing response shapes. Profile images use `foto_storage_key` resolved to a public URL; the avatar catalog (`IAvatarCatalog`, `MockAvatarCatalog`, `usuario.avatar_key`) was removed by decision — the frontend generates initials+color fallback when `fotoUrl` is null. Expanded scope adds authenticated multipart upload for catalog images backed by DigitalOcean Spaces through `AWSSDK.S3`; uploaded images are validated/transcoded to `.webp`, stored under server-generated keys, and persisted as object keys in existing `ImagenUrl` fields. Public responses continue using resolvers that accept either absolute URLs or Spaces keys.

## Architecture Decisions

| Decision | Choice | Alternatives considered | Rationale |
|---|---|---|---|
| Storage boundary | Add Application port `IFileStorageService`; Infrastructure implements `SpacesS3Storage` with `IAmazonS3` | Call S3 from controllers/handlers directly | Keeps AWS SDK and credentials outside Api/Application contracts. |
| Key generation | Server-side `StorageKeyFactory`: `products/{guid}.webp`, `recipes/{guid}.webp`; avatar keys removed by decision | Use uploaded filenames | Prevents path traversal, collisions, and user-controlled object paths. |
| Upload endpoint | `POST /api/productos/{id:guid}/imagen`, multipart `IFormFile`, protected by catalog-writer authorization | Allow any authenticated user to update `Producto.ImagenUrl` | Product catalog data is shared; unrestricted writes create integrity/BOLA risk. |
| S3 compatibility | Add `AWSSDK.S3`; configure endpoint override `https://nyc3.digitaloceanspaces.com`, bucket, region, public base URL, and credentials from config/env | DigitalOcean SDK/custom HTTP | AWS SDK supports S3-compatible APIs and keeps upload code standard. |
| Public reads | `PutObjectAsync` sets content type and `S3CannedACL.PublicRead`; resolver returns configured public URL | Proxy files through API | Public assets do not need app CPU/bandwidth once validated. |

## Data Flow

```text
POST /api/productos/{id}/imagen
  └─ Controller validates multipart presence/size/type
     └─ UploadProductImageHandler checks authorization + product existence
        └─ ImageSharp validates bytes, normalizes, writes WebP
           └─ StorageKeyFactory → products/{guid}.webp
              └─ IFileStorageService → Spaces PutObject(public-read)
                 └─ Producto.ImagenUrl = key → repository SaveChanges

GET profile/product/appliance/recipe → stored key or absolute URL → PublicAssetUrlResolver → response URL
```

## File Changes

| File | Action | Description |
|---|---|---|
| `src/Nido.Infrastructure/Nido.Infrastructure.csproj` | Modify | Add `AWSSDK.S3`. |
| `src/Nido.Application/Common/Storage/IFileStorageService.cs` | Create | Upload/delete storage port returning public URL metadata or throwing storage errors. |
| `src/Nido.Application/Common/Storage/StorageKeyFactory.cs` | Create | Generate entity-specific safe `.webp` keys. |
| `src/Nido.Infrastructure/Storage/SpacesOptions.cs` | Create | Bucket, endpoint, region, public base URL, access key, secret key, max upload bytes. |
| `src/Nido.Infrastructure/Storage/SpacesS3Storage.cs` | Create | `PutObjectAsync`/delete implementation using endpoint override and public-read ACL. |
| `src/Nido.Application/Common/Images/*` | Create | Common upload DTO + ImageSharp WebP processing contract shared by product/recipe image flows. |
| `src/Nido.Api/Controllers/ProductsController.cs` | Modify | Add `POST /api/productos/{id:guid}/imagen` with multipart binding and thin HTTP validation. |
| `src/Nido.Application/Productos/*UploadProductImage*` | Create | Validate product target, process image, upload, persist key. |
| `src/Nido.Infrastructure/Productos/ProductoRepository.cs` | Modify | Add update method for `Producto.ImagenUrl`; continue resolving keys on reads. |
| `DependencyInjection.cs`, `Program.cs`, `appsettings*.json` | Modify | Register `IAmazonS3`, options validation, storage/image services, and catalog-writer policy; commit only placeholders. |
| Avatar catalog files (removed) | Removed | `IAvatarCatalog`, `MockAvatarCatalog`, `IProfileImageUrlResolver`, `ProfileImageUrlResolver` were created but subsequently removed by decision. No `avatar_key` migration was applied. `foto_storage_key` remains the sole source for profile photos. |
| `src/Nido.Api/Controllers/PerfilController.cs` | Modify | Avatar-key fields removed; operates on `foto_storage_key` only. |
| `src/Nido.Application/UsuariosPerfil/ActualizarPerfilHandler.cs` | Modify | Avatar-key validation removed; handles `foto` upload only. |
| `src/Nido.Application/Auth/Register/RegisterUserHandler.cs` | Modify | Avatar-key validation removed; handles `foto` upload only. |

## Interfaces / Contracts

```csharp
public interface IFileStorageService
{
    Task<FileStorageUploadResult> UploadAsync(Stream stream, string key, string contentType, CancellationToken ct);
    Task DeleteAsync(string key, CancellationToken ct);
}

public sealed record FileStorageUploadResult(string Key, string PublicUrl);
```

`POST /api/productos/{id:guid}/imagen` accepts `multipart/form-data` field `file`. Success returns `200 OK` with the resolved image URL; validation failures return 400; unauthorized/forbidden return 401/403; missing product returns 404; Spaces failures return safe ProblemDetails without credentials.

## Testing Strategy

| Layer | What to Test | Approach |
|---|---|---|
| Unit | Key generation, URL resolution, upload validation | xUnit theories; no real Spaces calls. |
| Application | Upload handler saves key only after successful storage; rejects invalid target/auth | Mock storage/repository/image processor. |
| Infrastructure | `SpacesS3Storage` builds correct `PutObjectRequest` including bucket, key, content type, public-read ACL | Mock `IAmazonS3`. |
| Integration | Multipart endpoint status codes and persisted `ImagenUrl`; product/appliance/recipe reads resolve keys and preserve absolute URLs | WebApplicationFactory with fake `IFileStorageService`. |

## Migration / Rollout

No avatar migration was applied. `foto_storage_key` is the sole image source. Configure Spaces secrets via environment/secret store; `SpacesOptions` validates on startup when upload feature is enabled. Rollback removes upload endpoint and resolves `fotoUrl` directly (null or `foto_storage_key`); uploaded objects may remain orphaned unless manually cleaned.

## Open Questions

- [ ] Confirm the `CatalogImageWriter` authorization source/users before enabling product catalog image writes.
