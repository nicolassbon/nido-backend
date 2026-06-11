# user-profile-images Specification

## Purpose

Standardize how `fotoUrl` is resolved from `foto_storage_key` for profile and household member responses while preserving the existing real-photo upload flow.

## Requirements

### Requirement: Normalize registration profile images for storage

The system MUST process an accepted registration profile image into a normalized WebP asset before storing it.

#### Scenario: Accepted image is normalized before persistence

- GIVEN a registration request includes a valid JPG, PNG, or WebP `foto`
- WHEN the system accepts the image for registration
- THEN the stored asset is a WebP image
- AND the original uploaded bytes are not treated as the persisted profile image asset

### Requirement: Persist storage metadata independently of public URL

The system MUST generate a UUID-based `storageKey` before uploading a registration profile image, MUST persist that key separately from any public URL, and SHALL derive the public URL at runtime from configuration.

#### Scenario: Stored metadata keeps key independent from delivery domain

- GIVEN a registration request succeeds with a valid profile image
- WHEN the user record is persisted
- THEN the persisted profile image metadata includes the generated `storageKey`
- AND the persisted data does not depend on a hardcoded CDN or public domain value
- AND the system can derive the public image URL later from runtime configuration

### Requirement: Fail registration cleanly on upload or persistence errors

The system MUST not create a partial registration when profile image upload or persistence fails.

#### Scenario: Upload failure aborts registration

- GIVEN a registration request includes a valid profile image
- WHEN profile image upload fails before user persistence completes
- THEN the system rejects the request with an HTTP `5xx` server error
- AND the response is ProblemDetails or the project's equivalent server-error payload
- AND no new user account is persisted

#### Scenario: Persistence failure triggers compensating delete

- GIVEN a registration request uploads a profile image successfully
- AND user persistence fails afterward
- WHEN the failure is handled
- THEN the system attempts an immediate delete of the uploaded object
- AND the registration request fails
- AND no successful user registration response is returned

#### Scenario: Compensating delete failure is logged for later cleanup

- GIVEN a registration request uploads a profile image successfully
- AND user persistence fails afterward
- AND the immediate compensating delete also fails
- WHEN the failure is handled
- THEN the system logs the unrecovered cleanup failure with enough context to find the orphaned object later
- AND the registration request still fails

### Requirement: Clean up stored profile images after user deletion

The system SHOULD preserve profile image metadata for soft-deleted users until storage cleanup runs, and SHOULD perform asynchronous profile-image deletion from object storage with retries after user deletion is initiated.

> deferred-to: future change for async cleanup worker

#### Scenario: Soft-deleted user remains traceable for cleanup

_Deferred in this change. Kept as reference for future async cleanup implementation._

- GIVEN a user with a stored profile image is soft-deleted
- WHEN deletion is recorded in application data
- THEN the stored profile image metadata remains available for cleanup processing
- AND the user is not required to remain active for cleanup eligibility

#### Scenario: Storage cleanup retries after deletion

_Deferred in this change. Kept as reference for future async cleanup implementation._

- GIVEN a soft-deleted user still has a stored profile image object
- WHEN asynchronous cleanup attempts to remove the object from storage
- THEN the system retries failed deletions according to its cleanup policy
- AND successful cleanup removes the object from storage without requiring the user to be restored first

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