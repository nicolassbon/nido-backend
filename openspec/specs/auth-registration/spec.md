# auth-registration Specification

## Purpose

Define account registration that creates the initial user identity, accepts real profile-photo uploads, and preserves the existing silent-success duplicate-email behavior.

## Requirements

### Requirement: Register account with mandatory profile fields

The system MUST expose `POST /api/auth/register` as an anonymous `multipart/form-data` endpoint that accepts required fields `nombre`, `email`, `password`, and `sexo`, plus optional file field `foto`. The request MUST NOT accept `avatarKey` — avatar catalog was removed. A successful registration with valid data MUST return `201 Created` with the existing `RegisterResponse` shape.

#### Scenario: Successful registration without profile image returns created response

- GIVEN no authenticated session is required to register
- WHEN the client submits `multipart/form-data` to `POST /api/auth/register` with valid `nombre`, `email`, `password`, and `sexo` fields and omits `foto`
- THEN the system creates the user account
- AND the system returns HTTP `201 Created`
- AND the response payload contains `usuarioId`, `hogarId`, and `accessToken`
- AND the response does not require any authenticated caller identity

#### Scenario: Successful registration with valid profile image returns created response

- GIVEN no authenticated session is required to register
- WHEN the client submits `multipart/form-data` to `POST /api/auth/register` with valid `nombre`, `email`, `password`, `sexo`, and a valid `foto` file
- THEN the system creates the user account
- AND the system returns HTTP `201 Created`
- AND the response payload contains `usuarioId`, `hogarId`, and `accessToken`
- AND `foto_storage_key` is set for the created user

#### Scenario: Missing required field is rejected

- GIVEN a registration request with any required field omitted
- WHEN the request is validated
- THEN the system rejects the request with HTTP `400 Bad Request`
- AND the response is ProblemDetails describing the validation failure

#### Scenario: Legacy JSON registration payload is rejected

- GIVEN `POST /api/auth/register` only supports `multipart/form-data`
- WHEN a client submits the registration payload as `application/json`
- THEN the system rejects the request with an HTTP `4xx` client error
- AND the response indicates that the request contract is invalid for the endpoint

### Requirement: Preserve duplicate-email silent success

The system MUST return `200 OK` with the existing generic body when a registration request targets an email that already exists. The response MUST remain intentionally generic for security and MUST NOT leak whether the account already exists.

#### Scenario: Duplicate email returns silent success

- GIVEN an existing account with the same email
- WHEN a registration request is submitted for that email
- THEN the system returns HTTP `200 OK`
- AND the response body uses the existing generic silent-success shape
- AND the response does not expose `usuarioId`, `hogarId`, or `accessToken`

#### Scenario: Concurrent duplicate submissions allow only one created account

- GIVEN two registration submissions race with the same email and otherwise valid payloads
- WHEN the system processes both requests
- THEN at most one request succeeds with HTTP `201 Created`
- AND every losing request returns HTTP `200 OK` with the generic silent-success body
- AND the system does not persist multiple accounts for the same email

### Requirement: Validate optional registration profile image

The system MUST treat `foto` as optional and MUST only accept JPG, PNG, or WebP images up to 5 MB when the file is provided.

#### Scenario: Unsupported image format is rejected

- GIVEN a registration request includes `foto` with a non-JPG, non-PNG, and non-WebP image
- WHEN the file is validated
- THEN the system rejects the request with HTTP `400 Bad Request`
- AND the response is ProblemDetails describing the invalid file type
- AND the system does not create the user account

#### Scenario: Oversize image is rejected

- GIVEN a registration request includes `foto` larger than 5 MB
- WHEN the file is validated
- THEN the system rejects the request with HTTP `400 Bad Request`
- AND the response is ProblemDetails describing the size limit violation
- AND the system does not create the user account

#### Scenario: Corrupt image is rejected

- GIVEN a registration request includes `foto` whose bytes cannot be decoded as a valid image
- WHEN the image is processed
- THEN the system rejects the request with HTTP `400 Bad Request`
- AND the response is ProblemDetails describing the invalid image content
- AND the system does not create the user account