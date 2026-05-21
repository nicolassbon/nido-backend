---
name: docker-compose-patterns
description: 'Pragmatic Docker Compose guidance for local development with ASP.NET Core, Angular, MySQL, networking, volumes, healthchecks, and troubleshooting.'
---

# Docker Compose Patterns

Use this skill when working with Docker Compose for local development and reproducible demos.

The goal is to keep the environment simple, portable, and easy for the team to run.

Do **not** overcomplicate the setup.
Do **not** treat Compose as production orchestration.
Do **not** mix unrelated infrastructure unless the project actually needs it.

---

## When to Use

Use this skill for:

- Creating or reviewing `docker-compose.yml`
- Running ASP.NET Core + Angular + MySQL locally
- Configuring service networking
- Managing database volumes
- Adding healthchecks
- Handling environment variables
- Debugging container startup or connectivity issues
- Preparing a reproducible demo environment

---

## Core Principles

### 1. One Service per Concern

Prefer separate services for:

- Backend API
- Frontend app
- Database
- Optional tools such as Adminer, Mailpit, or Redis

Avoid one giant container running the whole application.

---

### 2. Use Service Names for Networking

Inside Docker Compose, containers communicate using service names.

For example, if the MySQL service is called `mysql`, the backend connection string should use `mysql` as the host, not `localhost`.

```txt
Server=mysql;Port=3306;Database=nido;User=nido_user;Password=nido_password;
```

Use `localhost` only when connecting from the host machine to an exposed port.

---

### 3. Persist Database Data with Named Volumes

Use named volumes for database persistence.

```yaml
volumes:
  mysql_data:
```

Avoid storing database data only inside the container, because it disappears when the container is recreated.

Be careful with:

```bash
docker compose down -v
```

This removes volumes and deletes local database data.

---

### 4. Use Healthchecks for Dependent Services

For databases, add a healthcheck and make the backend wait for the database to be healthy.

```yaml
services:
  mysql:
    image: mysql:8.4
    environment:
      MYSQL_DATABASE: nido
      MYSQL_USER: nido_user
      MYSQL_PASSWORD: nido_password
      MYSQL_ROOT_PASSWORD: root_password
    volumes:
      - mysql_data:/var/lib/mysql
    healthcheck:
      test: ["CMD", "mysqladmin", "ping", "-h", "localhost", "-u", "root", "-proot_password"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 20s

  api:
    build:
      context: ./backend
    depends_on:
      mysql:
        condition: service_healthy
```

Healthchecks reduce startup errors where the API starts before MySQL is ready.

---

### 5. Keep Secrets Out of Git

Do not hardcode real secrets in `docker-compose.yml`.

For local development, use `.env` files and keep them out of version control.

```yaml
services:
  api:
    env_file:
      - .env
```

Commit a safe example file instead:

```txt
.env.example
```

Never commit:

```txt
.env
.env.local
.env.production
```

---

### 6. Use Environment Variables for ASP.NET Core Configuration

ASP.NET Core maps nested configuration using double underscores.

```yaml
services:
  api:
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__DefaultConnection: Server=mysql;Port=3306;Database=nido;User=nido_user;Password=nido_password;
      Jwt__Issuer: Nido
      Jwt__Audience: NidoClient
```

Prefer environment variables over modifying `appsettings.json` for container-only values.

---

### 7. Expose Only Needed Ports

Expose ports only when the host machine needs access.

For example:

```yaml
services:
  api:
    ports:
      - "8080:8080"

  mysql:
    ports:
      - "3306:3306"
```

For a demo or local development, exposing MySQL can be useful for tools like DBeaver or MySQL Workbench.

For a more isolated setup, omit the database port and let only the API access it through the Docker network.

---

## Recommended Local Stack Example

Example for an ASP.NET Core API, Angular frontend, and MySQL database.

```yaml
services:
  api:
    build:
      context: ./backend
      dockerfile: Dockerfile
    container_name: nido-api
    ports:
      - "8080:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__DefaultConnection: Server=mysql;Port=3306;Database=nido;User=nido_user;Password=nido_password;
    depends_on:
      mysql:
        condition: service_healthy
    networks:
      - nido_network

  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile
    container_name: nido-frontend
    ports:
      - "4200:4200"
    depends_on:
      - api
    networks:
      - nido_network

  mysql:
    image: mysql:8.4
    container_name: nido-mysql
    ports:
      - "3306:3306"
    environment:
      MYSQL_DATABASE: nido
      MYSQL_USER: nido_user
      MYSQL_PASSWORD: nido_password
      MYSQL_ROOT_PASSWORD: root_password
    volumes:
      - mysql_data:/var/lib/mysql
    healthcheck:
      test: ["CMD", "mysqladmin", "ping", "-h", "localhost", "-u", "root", "-proot_password"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 20s
    networks:
      - nido_network

volumes:
  mysql_data:

networks:
  nido_network:
    driver: bridge
```

Adjust paths, ports, credentials, and service names based on the actual project.

---

## Development vs Demo Setup

For local development:

- Bind mounts can be useful for hot reload
- Expose database ports for debugging
- Use development environment variables
- Keep logs verbose enough to troubleshoot

For a demo setup:

- Prefer stable images and clean build steps
- Avoid requiring local SDKs/tools beyond Docker
- Use seeded data when useful
- Document startup commands clearly

---

## Useful Commands

Start services:

```bash
docker compose up
```

Start and rebuild:

```bash
docker compose up --build
```

Run in detached mode:

```bash
docker compose up -d
```

Stop services:

```bash
docker compose down
```

Stop services and delete volumes:

```bash
docker compose down -v
```

View logs:

```bash
docker compose logs -f api
```

Open a shell inside a container:

```bash
docker compose exec api sh
```

Check running services:

```bash
docker compose ps
```

Check resource usage:

```bash
docker stats
```

---

## Troubleshooting Checklist

### API Cannot Connect to MySQL

Check:

- The API connection string uses `mysql` as host, not `localhost`
- Both services are on the same Compose network
- MySQL credentials match the environment variables
- MySQL healthcheck is passing
- The database container finished initialization

Useful commands:

```bash
docker compose logs mysql

docker compose logs api
```

---

### Port Already in Use

Check which process is using the port or change the host port.

```yaml
ports:
  - "8081:8080"
```

This maps host port `8081` to container port `8080`.

---

### Database Data Looks Old or Broken

The named volume may contain old data.

For a full reset:

```bash
docker compose down -v

docker compose up --build
```

Only do this when it is safe to delete local database data.

---

### Frontend Cannot Reach API

Check whether the frontend is running:

- In the browser on the host machine
- Inside a container

From the browser, use the host-exposed API URL:

```txt
http://localhost:8080
```

From another container, use the service name:

```txt
http://api:8080
```

---

## Security Guidance

For local development:

- Do not commit real secrets
- Use `.env.example` for documentation
- Pin image versions when possible
- Avoid `latest` for important dependencies
- Do not run containers as root when avoidable
- Do not expose database ports unless needed

For production:

- Docker Compose is not a full production orchestration solution
- Use a real deployment platform or orchestrator when required
- Use managed secrets
- Configure HTTPS properly
- Avoid development credentials and debug settings

---

## Anti-Patterns

Avoid:

- Using `localhost` between containers
- Committing `.env` files
- Running all services in one container
- Storing database data without a volume
- Using `docker compose down -v` without understanding data loss
- Depending on startup order without healthchecks
- Exposing every internal service port by default
- Putting production secrets in Compose files
- Treating Compose as Kubernetes

---

## Review Checklist

When reviewing a Compose setup, check:

- Are services separated by responsibility?
- Does the API connect to MySQL using the service name?
- Are persistent services using named volumes?
- Are healthchecks present where useful?
- Are secrets kept out of Git?
- Are only necessary ports exposed?
- Is the setup easy for a teammate to run?
- Are environment variables clear and documented?
- Does `docker compose up --build` work from a clean clone?
