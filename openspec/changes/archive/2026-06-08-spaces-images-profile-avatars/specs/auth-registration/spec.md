# auth-registration Specification

## Purpose

Allow photo upload during registration without changing the current multipart contract or success/error semantics. Avatar catalog was removed by decision — registration accepts only `foto` for real photo uploads.

## Requirements

### Requirement: Registration accepts photo upload

`POST /api/auth/register` MUST remain anonymous and `multipart/form-data`. The request MUST continue accepting `nombre`, `email`, `password`, and `sexo`, and SHALL continue accepting optional `foto` as a file for the user's real profile photo. The request MUST NOT accept `avatarKey` — avatar catalog was removed. A new successful registration with a valid photo MUST keep returning `201 Created` with the existing `RegisterResponse` shape, and the duplicate-email silent-success path MUST keep returning `200 OK` with the existing generic body.

#### Scenario: Register with photo upload

- GIVEN an anonymous client sends valid multipart registration data with `foto`
- WHEN the upload satisfies existing profile-image validation
- THEN the API returns `201 Created` with the unchanged `RegisterResponse` payload
- AND `foto_storage_key` is set for the created user

#### Scenario: Register without photo

- GIVEN an anonymous client sends valid multipart registration data without `foto`
- WHEN all required fields are valid
- THEN the API returns `201 Created` with the unchanged `RegisterResponse` payload (tokens, no fotoUrl)
- AND the profile photo is resolved later via `GET /api/perfiles`
