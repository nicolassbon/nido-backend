# user-profile-images Specification

## Purpose

Standardize how `fotoUrl` is resolved from `foto_storage_key` for profile and household member responses. Avatar catalog was removed by decision — the backend stores only uploaded real photos, and the frontend generates initials+color fallback when no photo exists.

## Requirements

### Requirement: fotoUrl resolves from foto_storage_key only

For authenticated profile and household-member reads, the system MUST keep the `fotoUrl` response field name unchanged. `GET /api/perfiles` and household member responses from `GET /api/hogares/miembros` MUST resolve `fotoUrl` from `usuarios.foto_storage_key` when present, and MUST return `null` when no storage key is set.

#### Scenario: foto_storage_key present returns resolved URL

- GIVEN a user has `foto_storage_key` set
- WHEN profile or member data is returned
- THEN `fotoUrl` is the public URL resolved from the storage key

#### Scenario: No storage key returns null

- GIVEN a user has no `foto_storage_key`
- WHEN profile or member data is returned
- THEN `fotoUrl` is `null`

### Requirement: Profile update accepts photo upload (foto_storage_key only)

`PUT /api/perfiles` MUST keep its current `multipart/form-data` contract and `200 OK` success shape. The request SHALL accept optional `foto` for uploading a real profile photo. The request MUST NOT accept `avatarKey` — avatar catalog was removed.

#### Scenario: Update profile with photo upload

- GIVEN an authenticated user sends `PUT /api/perfiles` with valid profile fields and a `foto` file
- WHEN the upload is valid
- THEN `foto_storage_key` is updated with the new storage key
- AND subsequent profile reads resolve `fotoUrl` from the updated key
