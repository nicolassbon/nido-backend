# Apply Progress: Spaces Images & Profile Avatars

## Status

All phases completed under maintainer-approved `size:exception` for a single PR boundary. Avatar catalog (`IAvatarCatalog`, `MockAvatarCatalog`, `avatar_key`) was subsequently removed by decision — the backend now stores only `foto_storage_key` for real profile photos, and the frontend generates initials+color fallback.

## Completed Tasks (Post-Avatar Removal)

- [x] Phase 1 — Storage infrastructure: `AWSSDK.S3`, `IFileStorageService`, `SpacesOptions`, `SpacesS3Storage`, `StorageKeyFactory`, image processing contract, DI registration.
- [ ] ~~Phase 2 — Avatar catalog and URL resolution:~~ **REMOVED by decision.** Avatar migration, catalog, and profile resolvers were created and then reverted. `IPublicAssetUrlResolver` and `SpacesPublicAssetUrlResolver` kept for product/appliance image resolution.
- [x] Phase 3 — Registration and profile updates: cleaned up to accept `foto` only (no `avatarKey`). Profile/member `fotoUrl` resolves from `foto_storage_key` only.
- [x] Phase 4 — Upload endpoints: product, electrodoméstico, catalog, and recipe multipart image upload endpoints and handlers.
- [x] Phase 5 — Read resolution: product, manual product, electrodoméstico, catalog, and recipe image reads resolve storage keys at read time while preserving absolute/root-relative URLs.
- [x] Phase 6 — Error handling and validation: upload-specific exceptions map to 400/413/404/502/500; storage upload failure happens before DB updates.
- [x] Phase 7 — Tests: unit and integration coverage for key generation, cleanup key recognition, Spaces request construction, public asset resolver, product/electrodoméstico upload authorization, upload validation responses, and closed shared catalog/recipe write endpoints. Avatar-specific tests (7.3, 7.4, 7.9, 7.10) removed. Full build and test suite run.

## Avatar Removal Summary

| Artifact | Status |
|---|---|
| `IAvatarCatalog` | Removed |
| `MockAvatarCatalog` | Removed |
| `IProfileImageUrlResolver` | Removed |
| `ProfileImageUrlResolver` | Removed |
| `Usuario.AvatarKey` (domain) | Removed |
| `Usuario.AvatarKey` (entity) | Removed |
| `avatar_key` migration | Never applied |
| Registration `avatarKey` DTO field | Removed |
| Profile update `avatarKey` DTO field | Removed |
| `SpacesPublicAssetUrlResolver` | Kept (product/appliance images) |
| `IPublicAssetUrlResolver` | Kept |
| `foto_storage_key` | Sole image source for profiles |

## TDD Cycle Evidence

| Task group | Test file | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|---|---|---|---|---|---|---|---|
| 1.5 | `tests/Nido.Application.Tests/Common/Storage/StorageKeyFactoryTests.cs` | Unit | N/A (new) | ✅ Written before implementation | ✅ Passed in full suite | ✅ Prefix + uniqueness cases | ✅ Pure factory |
| 1.4 / 6.2 | `tests/Nido.Infrastructure.Tests/Storage/SpacesS3StorageTests.cs` | Unit | N/A (new) | ✅ Written before implementation | ✅ Passed in full suite | ✅ PutObject + missing-config cases | ✅ Moved config failure to upload-time to avoid breaking unrelated endpoints |
| 2.4 | ~~`tests/Nido.Infrastructure.Tests/ProfileImages/MockAvatarCatalogTests.cs`~~ | Unit | Removed | — | — | — | Avatar catalog removed |
| 2.6 | ~~`tests/Nido.Infrastructure.Tests/ProfileImages/ProfileImageUrlResolverTests.cs`~~ | Unit | Removed | — | — | — | Avatar resolver removed |
| 2.8 / 5.3 | `tests/Nido.Infrastructure.Tests/PublicAssets/SpacesPublicAssetUrlResolverTests.cs` | Unit | N/A (new) | ✅ Written before implementation | ✅ Passed in full suite | ✅ Absolute/root-relative passthrough + key + null | ✅ Resolver kept side-effect free |
| 4.1 / 6.3 | `tests/Nido.Application.Tests/Productos/UploadProductImageHandlerTests.cs` | Unit | N/A (new) | ✅ Written before implementation | ✅ Passed in full suite | ✅ Success + storage failure + not found | ✅ Common upload pattern reused for other targets |
| 4.1 / 4.3 / 6.1 / 7.7 / 7.8 | `tests/Nido.Api.IntegrationTests/ImageUploads/ImageUploadAuthorizationEndpointTests.cs` | Integration | Added during verify repair | ✅ Verify found missing runtime evidence | ✅ Passed targeted and full suite | ✅ Product success, cross-household denial, missing file, unsupported type, oversize, not found; electrodoméstico success + cross-household denial; catalog/recipe 403 | ✅ Closed shared writes until an admin/catalog-writer policy exists |
| 1.5 / cleanup | `tests/Nido.Application.Tests/Common/Storage/StorageKeyRulesTests.cs` | Unit | Added during verify repair | ✅ Verify found `electrodomesticos/` cleanup gap | ✅ Passed targeted and full suite | ✅ Managed prefixes include product, recipe, catalog, electrodoméstico, and profile keys | ✅ Cleanup rule now matches generated electrodoméstico keys |

## Verification

- ✅ `dotnet restore Nido.slnx`
- ✅ `dotnet build Nido.slnx --no-restore`
- ✅ `dotnet test tests/Nido.Application.Tests/Nido.Application.Tests.csproj --no-build` — 93 passed
- ✅ `dotnet test tests/Nido.Infrastructure.Tests/Nido.Infrastructure.Tests.csproj --no-build` — 36 passed
- ✅ `dotnet test tests/Nido.Api.IntegrationTests/Nido.Api.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~ImageUploadAuthorizationEndpointTests` — 10 passed
- ✅ `dotnet test Nido.slnx` — Application 93 passed, Infrastructure 36 passed, Api integration 110 passed; Domain project reports no tests available.
- ⏭️ Real DigitalOcean Spaces connectivity tests skipped: no live credentials were provided or committed by design.

## Deviations / Notes

- Avatar catalog (`IAvatarCatalog`, `MockAvatarCatalog`, `avatar_key`) was removed by user decision. Profile photos use `foto_storage_key` only. Frontend generates initials+color fallback.
- The app stores only storage keys in DB fields (`foto_storage_key`, `ImagenUrl`) and resolves URLs at read time.
- `Spaces.Enabled=false` in committed appsettings so missing credentials do not fail normal app startup; enabling upload config validates required fields on start, and upload attempts without config return the configured safe 500 ProblemDetails.
- Existing non-upload endpoints do not eagerly fail when Spaces credentials are absent; storage configuration is enforced at upload-time and startup-time only when explicitly enabled.
- Catalog and recipe image upload endpoints return 403 for normal authenticated users until a real admin/catalog-writer policy exists in the codebase.

## Workload / PR Boundary

- Mode: single PR with maintainer-approved `size:exception`
- Chain strategy: none
- Estimated review budget impact: high (~950-1100 lines), accepted by maintainer
