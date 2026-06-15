# Exploration: spaces-images-profile-avatars

## Current State

### Profile Image Architecture

The current profile image system uses local file storage with runtime URL resolution:

**Upload Flow (Registration + Profile Update)**:
1. `IProfileImageProcessor` (ImageSharpProfileImageProcessor) validates and processes images:
   - Accepts JPG, PNG, WebP
   - Max 5MB, max 4096px dimensions
   - Resizes to 512px max, converts to WebP (quality 80)
2. `IProfileImageStorage` (LocalProfileImageStorage) stores at `wwwroot/uploads/usuarios/{id}/profile/{guid}.webp`
3. `FotoStorageKey` persisted in `usuarios` table (e.g., `usuarios/{id}/profile/{guid}.webp`)
4. `IProfileImagePublicUrlResolver` (ConfigurableProfileImagePublicUrlResolver) resolves key → URL at runtime using `ProfileImageOptions.PublicBaseUrl`

**API Response Pattern**:
- `GET /api/perfiles` → resolves `fotoUrl` from `FotoStorageKey`
- `GET /api/hogares/miembros` → resolves `FotoUrl` from `FotoStorageKey` via `InvitacionRepository`
- Both use `IProfileImagePublicUrlResolver.Resolve(storageKey)`

**Key Files**:
- `src/Nido.Application/Common/ProfileImages/IProfileImageStorage.cs` — Upload/Delete interface
- `src/Nido.Application/Common/ProfileImages/IProfileImageProcessor.cs` — Image processing interface
- `src/Nido.Application/Common/ProfileImages/IProfileImagePublicUrlResolver.cs` — URL resolution interface
- `src/Nido.Infrastructure/ProfileImages/LocalProfileImageStorage.cs` — Local filesystem storage
- `src/Nido.Infrastructure/ProfileImages/ImageSharpProfileImageProcessor.cs` — ImageSharp processing
- `src/Nido.Infrastructure/ProfileImages/ConfigurableProfileImagePublicUrlResolver.cs` — Config-based URL resolver
- `src/Nido.Infrastructure/ProfileImages/ProfileImageOptions.cs` — Options (MaxBytes, MaxDimension, WebpQuality, PublicBaseUrl)
- `src/Nido.Application/UsuariosPerfil/ActualizarPerfilHandler.cs` — Profile update with image
- `src/Nido.Application/Auth/Register/RegisterUserHandler.cs` — Registration with image
- `src/Nido.Infrastructure/Hogares/InvitacionRepository.cs` — Members query with fotoUrl resolution

### Product/Electrodoméstico Image Architecture

**Current State**: These already use external URL strings stored directly in the database:
- `Producto.ImagenUrl` — direct URL in `productos` table
- `ElectrodomesticoCatalogo.ImagenUrl` — direct URL in `electrodomesticos_catalogo` table
- `Electrodomestico.ImagenUrl` — direct URL in `electrodomesticos` table (falls back to catalog)

**Key Files**:
- `src/Nido.Infrastructure/Persistence/Entities/Producto.cs` — Entity with ImagenUrl
- `src/Nido.Infrastructure/Persistence/Entities/ElectrodomesticoCatalogo.cs` — Catalog entity with ImagenUrl
- `src/Nido.Infrastructure/Electrodomesticos/ElectrodomesticoRepository.cs` — Fallback logic (electrodomestico → catalog by id → catalog by name)
- `src/Nido.Api/Contracts/Electrodomesticos/ElectrodomesticoResponse.cs` — Response with ImagenUrl

**Observation**: Product/electrodoméstico images are NOT uploaded by users — they come from seed data or catalog. No local storage involved. Moving to Spaces means changing where these URLs point, not changing upload flow.

### Database Schema (Usuario)

```sql
-- Current usuarios table (relevant columns)
usuarios (
  id UUID PRIMARY KEY,
  nombre TEXT NOT NULL,
  email TEXT NOT NULL,
  password_hash TEXT,
  oauth_provider TEXT,
  oauth_id TEXT,
  sexo TEXT NOT NULL,
  telefono TEXT,
  foto_storage_key TEXT,  -- Current: local storage key
  created_at TIMESTAMPTZ NOT NULL,
  updated_at TIMESTAMPTZ NOT NULL,
  alerta_vencimiento_dias INT NOT NULL DEFAULT 0
)
```

### DI Registration Pattern

```csharp
// src/Nido.Infrastructure/DependencyInjection.cs
services.AddOptions<ProfileImageOptions>().Bind(configuration.GetSection(ProfileImageOptions.SectionName));
services.AddScoped<IProfileImageProcessor, ImageSharpProfileImageProcessor>();
services.AddScoped<IProfileImageStorage, LocalProfileImageStorage>();
services.AddScoped<IProfileImagePublicUrlResolver, ConfigurableProfileImagePublicUrlResolver>();
```

---

## Affected Areas

### For Avatar Feature (Primary PR Scope)
- `src/Nido.Domain/Usuarios/Usuario.cs` — Add `AvatarKey` property
- `src/Nido.Infrastructure/Persistence/Entities/Usuario.cs` — Add `AvatarKey` column
- `src/Nido.Application/UsuariosPerfil/ActualizarPerfilHandler.cs` — Change from image upload to avatar selection
- `src/Nido.Application/Auth/Register/RegisterUserHandler.cs` — Change from image upload to avatar selection
- `src/Nido.Api/Controllers/PerfilController.cs` — Change endpoint from multipart file to avatar_key string
- `src/Nido.Api/Controllers/AuthController.cs` — Change registration from multipart to JSON (or keep compatible)
- `src/Nido.Api/Contracts/UsuariosPerfil/ActualizarPerfilRequest.cs` — Replace `IFormFile? Foto` with `string? AvatarKey`
- `src/Nido.Api/Contracts/Auth/RegisterRequest.cs` — Replace foto with avatar_key
- `src/Nido.Infrastructure/Hogares/InvitacionRepository.cs` — Resolve fotoUrl from avatar_key
- `src/Nido.Application/Hogares/IInvitacionRepository.cs` — MiembroInfo may need avatar context
- `src/Nido.Infrastructure/DependencyInjection.cs` — Register avatar catalog service
- Migration file — Add `avatar_key` column to `usuarios`

### For Spaces Integration (Future PR)
- `src/Nido.Infrastructure/Persistence/Entities/Producto.cs` — ImagenUrl already exists
- `src/Nido.Infrastructure/Persistence/Entities/ElectrodomesticoCatalogo.cs` — ImagenUrl already exists
- Seed data files — Update URLs to Spaces URLs
- `ProfileImageOptions` or new `SpacesOptions` — Add endpoint/bucket config

---

## Approaches

### Approach 1: Avatar-Only for Profile (Recommended for PR1)

Replace profile image uploads with predefined avatar selection. No file upload, no local storage, no image processing for profiles.

**Implementation**:
1. Add `avatar_key TEXT` column to `usuarios` table (nullable)
2. Create `IAvatarCatalog` interface + `StaticAvatarCatalog` implementation with mock avatar definitions
3. Avatar catalog returns list of `{key, name, url}` where URL points to bundled/static avatar images
4. `ActualizarPerfilRequest` changes from `IFormFile? Foto` to `string? AvatarKey`
5. `RegisterUserRequest` changes from foto upload to `string? AvatarKey`
6. `ActualizarPerfilHandler` validates avatar_key against catalog, stores key
7. `RegisterUserHandler` validates avatar_key against catalog, stores key
8. `IProfileImagePublicUrlResolver` updated to resolve avatar_key → URL (or new resolver)
9. API responses still expose `fotoUrl` (resolved from avatar_key)
10. Old `FotoStorageKey` remains for backward compatibility (existing users keep their photos)

**Pros**:
- Eliminates file upload complexity for profiles
- No storage infrastructure needed for avatars
- Simpler security surface (no file upload attacks)
- Under 800 lines easily
- Clean separation: avatars for users, Spaces for products

**Cons**:
- Users lose ability to upload custom photos (product decision accepted)
- Need to host avatar images somewhere (bundled or CDN)

**Effort**: Medium (~400-500 lines)

### Approach 2: Spaces for Everything

Move all image handling (profiles + products) to DigitalOcean Spaces in one PR.

**Implementation**:
1. Add `SpacesOptions` with endpoint, buckets, credentials config
2. Implement `IProfileImageStorage` → `SpacesProfileImageStorage` using S3-compatible API
3. Add avatar catalog (same as Approach 1)
4. Update product/electrodoméstico seed data to use Spaces URLs
5. Update `IProfileImagePublicUrlResolver` for Spaces URLs

**Pros**:
- Unified storage backend
- One migration for all image handling

**Cons**:
- Exceeds 800 line budget significantly
- Mixes profile avatar change with product image migration
- Higher risk, more moving parts
- Needs S3-compatible SDK (new dependency)

**Effort**: High (~1000+ lines)

### Approach 3: Hybrid — Avatars + Spaces Config Foundation

Avatar feature for profiles + Spaces configuration/options for future product migration (no product changes yet).

**Implementation**:
- Same as Approach 1 for avatars
- Add `SpacesOptions` configuration class (endpoint, access key, secret key, buckets)
- Add `ISpacesStorage` interface (but no implementation yet)
- Document product image migration as follow-up

**Pros**:
- Sets up Spaces config for next PR
- Still under 800 lines
- Clear separation of concerns

**Cons**:
- Adds unused config/interface (slight over-engineering)
- Credentials handling needs careful thought even if not used yet

**Effort**: Medium-High (~600-700 lines)

---

## Recommendation

**Use Approach 1: Avatar-Only for Profile**.

Rationale:
1. **Scope discipline**: The user explicitly said "one PR under 800 lines". Avatars alone fit cleanly.
2. **Product images are already external**: `Producto.ImagenUrl` and `ElectrodomesticoCatalogo.ImagenUrl` are already URL strings — they just need to point to Spaces URLs in seed data. That's a data migration, not a code change.
3. **No S3 SDK needed yet**: For avatars, we don't need the S3-compatible SDK. For product images, the URLs are already stored — we just need to update the seed data URLs.
4. **Clear follow-up**: PR2 can be "Move product/electrodoméstico image URLs to Spaces" which is primarily seed data + config, not architecture change.

**Avatar Catalog Design**:
```csharp
public interface IAvatarCatalog
{
    IReadOnlyList<AvatarDefinition> GetAll();
    AvatarDefinition? FindByKey(string key);
    bool IsValidKey(string key);
}

public sealed record AvatarDefinition(
    string Key,        // e.g., "avatar_ninja", "avatar_chef"
    string Name,       // Display name
    string Url,        // URL to avatar image (bundled or CDN)
    string? Category); // Optional grouping (e.g., "cooking", "animals")
```

**Mock Avatars for Now**:
- 8-12 predefined avatars with descriptive keys
- Images can be bundled in `wwwroot/avatars/` or served from a placeholder CDN
- Catalog is static (hardcoded list) — can be moved to DB later if needed

**API Compatibility**:
- `GET /api/perfiles` → `fotoUrl` resolves from `avatar_key` if set, falls back to `foto_storage_key` for existing users
- `GET /api/hogares/miembros` → Same resolution logic
- Response shape unchanged — frontend sees `fotoUrl` regardless of source

---

## Risks

### 1. Backward Compatibility for Existing Users
- **Risk**: Existing users have `FotoStorageKey` set. If we only add `AvatarKey`, we need resolution logic that checks both.
- **Mitigation**: `IProfileImagePublicUrlResolver` (or new `IFotoUrlResolver`) checks `AvatarKey` first, falls back to `FotoStorageKey`. Old photos continue working.

### 2. Registration Contract Change
- **Risk**: Changing registration from multipart/form-data to JSON breaks frontend immediately.
- **Mitigation**: Option A: Keep multipart but accept optional `avatarKey` field alongside optional `foto` (deprecate foto). Option B: Add new JSON endpoint, keep old multipart working. **Recommend Option A for backward compat**.

### 3. Avatar Image Hosting
- **Risk**: Where do avatar images live? Bundled in app = redeploy to add avatars. CDN = external dependency.
- **Mitigation**: Start with bundled in `wwwroot/avatars/`. Move to CDN later. Catalog service abstracts this.

### 4. Avatar Key Validation
- **Risk**: Frontend sends invalid avatar_key.
- **Mitigation**: `IAvatarCatalog.IsValidKey()` validation before persistence. Return 400 on invalid key.

### 5. Spaces Credentials Security
- **Risk**: Credentials must NEVER be committed. User explicitly stated this.
- **Mitigation**: Use `.env` file (already in `.gitignore`), `IConfiguration` binding, `UserSecrets` for local dev. Document in `.env.example` with placeholder values only.

### 6. Migration Rollback
- **Risk**: Adding `avatar_key` column is non-destructive (nullable). But if we later remove `foto_storage_key`, that's destructive.
- **Mitigation**: Keep `foto_storage_key` for now. Mark as deprecated. Remove in future migration after all users migrated.

---

## Required Migrations/Contracts/Tests

### Migration
```sql
-- Add avatar_key column (nullable, non-destructive)
ALTER TABLE usuarios ADD COLUMN avatar_key TEXT;
-- No data migration needed — existing users keep foto_storage_key
```

### Contracts (DTOs)
- `ActualizarPerfilRequest` — Replace `IFormFile? Foto` with `string? AvatarKey` (or add both, deprecate Foto)
- `RegisterRequest` — Add `string? AvatarKey` field
- `AvatarResponse` — New: `{key, name, url, category}` for catalog endpoint
- `MiembroResponse` — No change (still has `FotoUrl`)
- `PerfilController` response — No change (still has `fotoUrl`)

### New Interfaces
- `IAvatarCatalog` — Avatar catalog service
- `IFotoUrlResolver` — Unified resolver (avatar_key → url, foto_storage_key → url)

### Tests
- Unit: `ActualizarPerfilHandler` with avatar_key validation
- Unit: `RegisterUserHandler` with avatar_key
- Unit: `AvatarCatalog` — all keys valid, invalid key rejected
- Unit: `FotoUrlResolver` — avatar priority, fallback to storage key
- Integration: `PUT /api/perfiles` with avatar_key
- Integration: `POST /auth/register` with avatar_key
- Integration: `GET /api/hogares/miembros` returns resolved fotoUrl

---

## Ready for Proposal

**Yes** — the analysis is complete and the codebase has been thoroughly explored.

### Open Decisions for Proposal
1. **Registration format**: Keep multipart (add avatarKey field) or switch to JSON (breaking change)?
2. **Avatar image hosting**: Bundled in `wwwroot/avatars/` or external CDN?
3. **Avatar catalog size**: How many mock avatars for initial implementation?
4. **Old foto_storage_key**: Deprecate now or keep indefinitely?
5. **Spaces config**: Add in this PR (foundation) or defer to product image PR?
