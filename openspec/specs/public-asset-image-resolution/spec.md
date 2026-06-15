# public-asset-image-resolution Specification

## Purpose

Define upload and read-time URL resolution for product and electrodoméstico images backed by DigitalOcean Spaces.

## Requirements

### Requirement: Product and appliance image uploads use multipart contracts

The system MUST support product and electrodoméstico image writes through `POST` or `PUT` `multipart/form-data` requests that include one image file, MUST upload accepted files to DigitalOcean Spaces through the AWS S3 SDK, and MUST return the resolved public URL in the success payload.

#### Scenario: Multipart upload succeeds

- GIVEN a valid product or electrodoméstico write request with one supported image file
- WHEN the API accepts the multipart request and uploads the file
- THEN the request succeeds with HTTP `200 OK` or `201 Created`
- AND the response includes the resolved public `ImagenUrl`

#### Scenario: Invalid file is rejected

- GIVEN a multipart write request includes a non-image or unreadable file
- WHEN file validation runs before upload
- THEN the API returns HTTP `400 Bad Request`
- AND the response is ProblemDetails describing the invalid file

#### Scenario: Size limit is enforced

- GIVEN a multipart write request includes an image above the configured size limit
- WHEN the file is validated
- THEN the API returns HTTP `413 Payload Too Large`
- AND no object is uploaded

### Requirement: Stored image metadata stays key-based

The system MUST generate a unique storage key before upload, SHALL place the object under a domain folder (`products/`, `recipes/`, `avatars/`, or `catalog/`), MUST persist only that storage key in the database field `ImagenUrl`, and MUST resolve public URLs at read time instead of storing absolute URLs.

#### Scenario: Product image stores generated key

- GIVEN a product image upload is accepted
- WHEN the object key is created
- THEN the key matches the product folder convention such as `products/{guid}.{ext}`
- AND the persisted `ImagenUrl` value is that storage key, not an absolute URL

#### Scenario: Read model resolves persisted key

- GIVEN a product or electrodoméstico record stores a non-empty image storage key
- WHEN the API returns that resource
- THEN the response resolves `ImagenUrl` to a public URL from runtime Spaces configuration

### Requirement: Spaces configuration is environment-bound and secret-safe

The system MUST read the Spaces endpoint `https://nyc3.digitaloceanspaces.com`, the environment-selected bucket (`nido-dev` or `nido-prod`), and access credentials from runtime configuration or environment variables only. The repository MUST NOT contain live access keys or secret keys.

#### Scenario: Repository config stays secret-free

- GIVEN application configuration files or examples are committed
- WHEN Spaces settings are defined there
- THEN they contain placeholders or bindings only
- AND no live credential is present in source control

#### Scenario: Missing upload configuration fails safely

- GIVEN an upload request needs Spaces configuration
- WHEN the endpoint, bucket, or credentials are missing or invalid
- THEN the API returns an HTTP `5xx` ProblemDetails error
- AND no database write commits an unusable image key

### Requirement: Upload failures do not create partial image state

The system MUST fail the request if Spaces upload fails and SHALL avoid persisting a storage key that was not uploaded successfully.

#### Scenario: Storage upload failure aborts write

- GIVEN a valid multipart image request
- WHEN the Spaces upload operation fails
- THEN the API returns an HTTP `5xx` ProblemDetails error
- AND the related product or electrodoméstico image field is not persisted with a new key