# Proposal: Spaces Images & Profile Avatars

## Intent

Reduce profile-image complexity and prepare public asset resolution for Spaces without exceeding one PR or changing storage credentials handling.

## Scope

### In Scope
- Add `usuarios.avatar_key` for predefined avatars and validate against a mock catalog (8-12 options).
- Keep `fotoUrl` in profile/member responses, resolving `avatar_key` first and legacy `foto_storage_key` second.
- Accept `avatarKey` in registration/profile update; keep `foto` only as deprecated compatibility input if needed.
- Add DigitalOcean Spaces options/resolver foundation for product/electrodoméstico image keys vs absolute URLs.

### Out of Scope
- New upload endpoint, S3/Spaces SDK integration, or bucket write flow.
- Removing `foto_storage_key` or migrating existing user photos in this PR.

## Capabilities

### New Capabilities
- `public-asset-image-resolution`: Resolve product/electrodoméstico image values as absolute URLs or storage keys using Spaces public config.

### Modified Capabilities
- `auth-registration`: Registration accepts avatar selection without breaking current HTTP semantics; `foto` becomes deprecated compatibility.
- `user-profile-images`: Profile/media behavior shifts to catalog avatars first, legacy uploaded photos second, while preserving `fotoUrl` outputs.

## Approach

Keep current controller contracts compatible by extending form payloads with `avatarKey`, introduce a static avatar catalog plus unified URL resolver, and add options-based Spaces public URL composition for catalog assets only. Credentials stay environment/config bound and never committed.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Nido.Domain/Usuarios/Usuario.cs` | Modified | Add avatar state and update semantics |
| `src/Nido.Api/Controllers/AuthController.cs` | Modified | Accept/deprecate registration media inputs |
| `src/Nido.Api/Controllers/PerfilController.cs` | Modified | Accept avatar selection and preserve `fotoUrl` |
| `src/Nido.Infrastructure/ProfileImages/` | Modified | Add avatar/catalog + resolver foundation |
| `src/Nido.Infrastructure/*/{Producto,Electrodomestico}*` | Modified | Resolve image string as URL or storage key |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Frontend contract confusion during deprecation | Med | Preserve multipart semantics and document `foto` deprecation |
| Invalid avatar keys persisted | Low | Validate against catalog before save |
| Misconfigured public asset URLs | Med | Centralize options validation and fallback behavior |

## Rollback Plan

Revert the migration adding `avatar_key`, disable avatar resolution paths, and keep existing `foto_storage_key`-only behavior plus absolute `ImagenUrl` passthrough.

## Dependencies

- DB migration for `usuarios.avatar_key`
- Public avatar assets and Spaces config placeholders

## Success Criteria

- [ ] Registration/profile update accept `avatarKey` with documented compatibility semantics.
- [ ] `fotoUrl` resolves avatar-first, then legacy photo, with no response shape change.
- [ ] Product/electrodoméstico image resolution supports absolute URLs and Spaces-backed keys without adding upload infrastructure.
