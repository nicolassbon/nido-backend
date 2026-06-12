# spaces-image-upload Specification

## Purpose

Enable product, electrodoméstico, recipe, and catalog image uploads to DigitalOcean Spaces through S3-compatible multipart endpoints, storing only storage keys in the database and resolving public URLs at read time.

## Requirements

### Requirement: Multipart image upload endpoints

The system MUST expose upload endpoints accepting `multipart/form-data` with an image file part. Each endpoint SHALL accept exactly one image per request. On success, the endpoint MUST return `200 OK` with the resolved public URL for the stored image. Upload endpoints SHALL require authentication and household membership.

The following upload paths MUST be available:

| Path | Folder | Entity field |
|---|---|---|
| `POST /api/productos/{id}/imagen` | `products/` | `productos.ImagenUrl` |
| `POST /api/electrodomesticos/{id}/imagen` | `electrodomesticos/` | `electrodomesticos.ImagenUrl` |
| `POST /api/catalogo-electrodomesticos/{id}/imagen` | `catalog/` | `electrodomesticos_catalogo.ImagenUrl` |
| `POST /api/recetas/{id}/imagen` | `recipes/` | `recetas.ImagenUrl` |

#### Scenario: Upload product image

- GIVEN an authenticated user sends `POST /api/productos/{id}/imagen` with a valid JPG, PNG, or WebP file in the `imagen` form field
- WHEN the file is under the configured size limit and upload to Spaces succeeds
- THEN the API returns `200 OK` with `{ "imagenUrl": "https://nido-dev.nyc3.digitaloceanspaces.com/products/{guid}.jpg" }`
- AND `productos.ImagenUrl` is persisted as `products/{guid}.jpg`

#### Scenario: Upload replaces existing image

- GIVEN a product already has an `ImagenUrl` storage key
- WHEN a new image is uploaded
- THEN the old object SHALL be deleted from Spaces and the new key stored

### Requirement: Storage key generation

The backend MUST generate unique storage keys using the pattern `{folder}/{guid}{extension}`. The guid SHALL be a valid v4 UUID. The extension SHALL be derived from the uploaded file's content type (`.jpg`, `.png`, `.webp`), not the client-provided filename.

#### Scenario: Key uniqueness

- GIVEN two image uploads to the same resource
- WHEN both complete
- THEN each receives a distinct storage key

### Requirement: Spaces upload via S3-compatible SDK

The backend MUST upload files to DigitalOcean Spaces using the AWS S3 SDK configured with the `nyc3.digitaloceanspaces.com` endpoint. The bucket MUST be selected from configuration (`nido-prod` for production, `nido-dev` otherwise). Files MUST be uploaded with `public-read` ACL so the public base URL works directly. The S3 SDK package SHALL be the `AWSSDK.S3` NuGet package.

#### Scenario: Spaces configuration via environment

- GIVEN the application starts with Spaces access key, secret key, endpoint, and bucket in configuration or environment variables
- WHEN an upload is requested
- THEN the S3 client uses those values without any hardcoded credentials

### Requirement: Credential security

Spaces access keys and secret keys MUST be read only from configuration sources (appsettings, environment variables, user secrets) and MUST NOT be present in any committed repository file. Example configuration files SHALL contain only placeholder values.

#### Scenario: No credentials in source control

- GIVEN the repository contains Spaces configuration examples or templates
- WHEN scanned for credentials
- THEN no live access keys or secret keys are found

### Requirement: Error handling

The system MUST handle the following upload failure modes with appropriate HTTP status codes and user-facing messages in Spanish:

| Condition | Status | Message |
|---|---|---|
| Missing image file part | `400 Bad Request` | "No se envió ninguna imagen." |
| Unsupported file type | `400 Bad Request` | "Formato de imagen no soportado. Use JPG, PNG o WebP." |
| File exceeds size limit | `413 Payload Too Large` | "La imagen excede el tamaño máximo permitido." |
| Spaces upload failure | `502 Bad Gateway` | "Error al guardar la imagen. Intente nuevamente." |
| Missing Spaces configuration | `500 Internal Server Error` | "El servicio de imágenes no está configurado." |
| Resource not found | `404 Not Found` | Entity-specific not-found message |

#### Scenario: Upload with unsupported file type

- GIVEN an authenticated user sends `POST /api/productos/{id}/imagen` with a PDF file
- WHEN the file type validation runs
- THEN the API returns `400 Bad Request` and the file is not uploaded

### Requirement: Bucket folder structure

Uploaded images MUST be organized into the following prefix hierarchy within the configured Spaces bucket:

- `products/` — Product images
- `electrodomesticos/` — Electrodoméstico images
- `recipes/` — Recipe images
- `avatars/` — Predefined avatar catalog images (seeded, not user-uploaded)
- `catalog/` — Electrodoméstico catalog images

#### Scenario: Product image stored under correct prefix

- GIVEN an image is uploaded for a product
- WHEN the storage key is generated
- THEN the key begins with `products/`
