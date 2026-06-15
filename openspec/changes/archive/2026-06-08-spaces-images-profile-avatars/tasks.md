# Tasks: Spaces Images & Profile Avatars

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~950-1100 |
| 800-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 → PR 2 → PR 3 |
| Delivery strategy | exception-ok (maintainer-approved size:exception) |
| Chain strategy | none |

Decision needed before apply: Resolved — maintainer approved size:exception
Chained PRs recommended: Yes, but overridden by approved size exception
Chain strategy: none
800-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Storage infrastructure + key generation + DI foundation | PR 1 | Base: feature branch. Includes S3 client, SpacesS3Storage, StorageKeyFactory, SpacesOptions, IFileStorageService interface, DI registration. Tests for storage layer. ~250-300 lines. |
| 2 | Avatar catalog + URL resolvers + profile/auth updates + DB migration | PR 2 | Base: PR 1 branch. Includes avatar_key migration, MockAvatarCatalog, ProfileImageUrlResolver, PublicAssetUrlResolver, controller/handler changes. Tests for resolvers and profile flows. ~350-400 lines. |
| 3 | Upload endpoints + handlers + integration tests | PR 3 | Base: PR 2 branch. Includes product/electro/catalog/recipe upload endpoints, UploadProductImageHandler, validation, error handling, end-to-end tests. ~350-400 lines. |

**Decision: Avatar catalog (PR 2 scope) was removed.** The simplified design keeps `foto_storage_key` only for profile photos; the frontend generates initials+color fallback. All avatar-specific interfaces, resolvers, and `avatar_key` migration are removed. Upload endpoints (PR 3) and storage infrastructure (PR 1) remain.

## Phase 1: Storage Infrastructure

- [x] 1.1 Add `AWSSDK.S3` NuGet package to `src/Nido.Infrastructure/Nido.Infrastructure.csproj`
- [x] 1.2 Create `src/Nido.Application/Common/Storage/IFileStorageService.cs` with `UploadAsync(Stream, string key, string contentType, CancellationToken)` and `DeleteAsync(string key, CancellationToken)` returning `FileStorageUploadResult(Key, PublicUrl)`
- [x] 1.3 Create `src/Nido.Infrastructure/Storage/SpacesOptions.cs` with `Bucket`, `Endpoint`, `Region`, `PublicBaseUrl`, `AccessKey`, `SecretKey`, `MaxUploadBytes` properties; bind from `Spaces` config section
- [x] 1.4 Create `src/Nido.Infrastructure/Storage/SpacesS3Storage.cs` implementing `IFileStorageService` using `IAmazonS3` with endpoint override, `public-read` ACL, and content type on `PutObjectRequest`
- [x] 1.5 Create `src/Nido.Application/Common/Storage/StorageKeyFactory.cs` with `GenerateProductKey()`, `GenerateRecipeKey()`, `GenerateCatalogKey()` returning `{folder}/{guid}.webp` (avatar key generation removed)
- [x] 1.6 Create `src/Nido.Application/Common/Images/ImageProcessingService.cs` contract for validation (MIME check, size limit) and WebP normalization
- [x] 1.7 Register `IAmazonS3` client, `SpacesOptions`, `IFileStorageService`, `StorageKeyFactory`, and `ImageProcessingService` in `DependencyInjection.cs`

## Phase 2: Avatar Catalog & URL Resolution — REMOVED

Avatar catalog was removed by decision. The following tasks were implemented but subsequently reverted:

- ~~[ ] 2.1 Create EF migration adding nullable `avatar_key varchar(128)` to `usuarios` table~~ **REMOVED — no migration was applied**
- ~~[ ] 2.2 Add `AvatarKey` property to entities~~ **REMOVED — entity properties were removed**
- ~~[ ] 2.3 Create `src/Nido.Application/Common/ProfileImages/IAvatarCatalog.cs`~~ **REMOVED — interface deleted**
- ~~[ ] 2.4 Create `src/Nido.Infrastructure/ProfileImages/MockAvatarCatalog.cs`~~ **REMOVED — mock deleted**
- ~~[ ] 2.5 Create `src/Nido.Application/Common/ProfileImages/IProfileImageUrlResolver.cs`~~ **REMOVED — interface deleted**
- ~~[ ] 2.6 Create `src/Nido.Infrastructure/ProfileImages/ProfileImageUrlResolver.cs`~~ **REMOVED — resolver deleted**
- [x] 2.7 Create `src/Nido.Application/Common/Assets/IPublicAssetUrlResolver.cs` with `Resolve(string? value)` for absolute URL passthrough or Spaces key resolution — **KEPT (used by product/appliance images)**
- [x] 2.8 Create `src/Nido.Infrastructure/PublicAssets/SpacesPublicAssetUrlResolver.cs`: absolute URL passthrough, key→PublicBaseUrl/key, null→null — **KEPT**
- [x] 2.9 Add `SpacesPublicAssets:PublicBaseUrl` placeholder to `appsettings.json` — **KEPT**

## Phase 3: Registration & Profile Updates

Avatar-related handlers were cleaned up. The registration and profile update endpoints now handle `foto` (real photo upload) only.

- [x] 3.1 Registration DTO and controller accept `foto` for photo upload (no avatarKey) — **avatarKey removed**
- ~~[ ] 3.2 Update registration handler: validate `avatarKey` via `IAvatarCatalog.Exists()`~~ **REMOVED — no avatar catalog to validate against**
- [x] 3.3 Profile update DTO and controller accept `foto` for photo upload (no avatarKey) — **avatarKey removed**
- ~~[ ] 3.4 Update profile handler: validate `avatarKey` via catalog~~ **REMOVED**
- [x] 3.5 Update `GET /api/perfiles` to resolve `fotoUrl` from `foto_storage_key` only (no avatar-first logic) — **simplified**
- [x] 3.6 Update `GET /api/hogares/miembros` member mapping to resolve `fotoUrl` from `foto_storage_key` only — **simplified**

## Phase 4: Upload Endpoints

- [x] 4.1 Create `src/Nido.Application/Productos/UploadProductImage/UploadProductImageCommand` and `Handler`: validate product exists + household access, process image, generate key, upload via `IFileStorageService`, persist `Producto.ImagenUrl`
- [x] 4.2 Add `POST /api/productos/{id:guid}/imagen` to `ProductsController` with multipart binding, thin HTTP validation (presence, size, MIME)
- [x] 4.3 Create `src/Nido.Application/Electrodomesticos/UploadElectrodomesticoImage/` command + handler following same pattern
- [x] 4.4 Add `POST /api/electrodomesticos/{id}/imagen` endpoint
- [x] 4.5 Create `src/Nido.Application/CatalogoElectrodomesticos/UploadCatalogImage/` command + handler
- [x] 4.6 Add `POST /api/catalogo-electrodomesticos/{id}/imagen` endpoint
- [x] 4.7 Create `src/Nido.Application/Recetas/UploadRecipeImage/` command + handler
- [x] 4.8 Add `POST /api/recetas/{id}/imagen` endpoint
- [x] 4.9 Update repositories to delete old Spaces object before storing new key on re-upload

## Phase 5: Product/Appliance Read Resolution

- [x] 5.1 Update `ProductoRepository` response mapping to resolve `ImagenUrl` through `IPublicAssetUrlResolver.Resolve()`
- [x] 5.2 Update `ElectrodomesticoRepository` response mapping to resolve `ImagenUrl` through resolver
- [x] 5.3 Ensure absolute URLs pass through unchanged per spec

## Phase 6: Error Handling & Validation

- [x] 6.1 Add upload error responses: 400 missing file, 400 unsupported type, 413 size exceeded, 502 Spaces failure, 500 missing config, 404 resource not found
- [x] 6.2 Ensure `SpacesOptions` validation on startup when upload feature is enabled; fail fast with clear message
- [x] 6.3 Ensure no DB write commits if Spaces upload fails (atomicity)

## Phase 7: Tests

- [x] 7.1 Unit test `StorageKeyFactory`: generates unique keys with correct folder prefix and `.webp` extension
- [x] 7.2 Unit test `SpacesS3Storage`: builds correct `PutObjectRequest` with bucket, key, content type, public-read ACL (mock `IAmazonS3`)
- ~~[ ] 7.3 Unit test `MockAvatarCatalog`: valid keys return true/storageKey, invalid return false/null~~ **REMOVED — mock deleted**
- ~~[ ] 7.4 Unit test `ProfileImageUrlResolver`: avatar priority, legacy fallback, null when neither~~ **REMOVED — resolver deleted**
- [x] 7.5 Unit test `SpacesPublicAssetUrlResolver`: absolute passthrough, key resolution, null handling — **KEPT**
- [x] 7.6 Unit test `UploadProductImageHandler`: saves key only after successful storage; rejects invalid target/auth
- [x] 7.7 Integration test `POST /api/productos/{id}/imagen` success returns 200 with resolved URL; persists key
- [x] 7.8 Integration test upload endpoints: 400 missing file, 400 bad type, 413 oversized, 404 not found
- ~~[ ] 7.9 Integration test `POST /api/auth/register` with valid `avatarKey` returns 201~~ **REMOVED — no avatarKey in registration**
- ~~[ ] 7.10 Integration test `PUT /api/perfiles` with valid `avatarKey` returns 200; subsequent GET resolves avatar~~ **REMOVED — no avatarKey in profiles**
- [x] 7.11 Integration test product/appliance reads resolve Spaces keys but preserve absolute URLs — **KEPT**
