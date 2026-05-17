# Neco Board CE — API Server

**Neco Board Community Edition** is an open-source collaborative kanban board application with real-time updates, role-based access control, and flexible storage backends.

| Component | Repository / Image |
|-----------|-------------------|
| **API (this repo)** | Docker Hub: [ren4el/neco-board-api](https://hub.docker.com/repository/docker/ren4el/neco-board-api/general) |
| **Client** | GitHub: [NecoCore/Neco-Board-CE-Client](https://github.com/NecoCore/Neco-Board-CE-Client) · Docker Hub: [ren4el/neco-board-client](https://hub.docker.com/repository/docker/ren4el/neco-board-client/general) |

---

## Features

- JWT authentication with refresh token rotation
- Real-time collaboration via SignalR WebSockets
- Role-based access control per project
- File attachments (local filesystem or AWS S3)
- Multiple database backends (SQLite, PostgreSQL, MySQL, SQL Server)
- Interactive API documentation at `/scalar`

---

## Quick Start

### Docker Compose (recommended)

Create a `docker-compose.yml`:

```yaml
services:
  api:
    image: ren4el/neco-board-api:latest
    ports:
      - "8080:8080"
    environment:
      APP_ALLOW_ORIGINS: "http://localhost:3000"
      JWT_SECRET: "your-secret-key-minimum-32-characters"
      JWT_ISSUER: "neco-board"
      JWT_AUDIENCE: "neco-board"
      DATABASE_TYPE: "sqlite"
      FILE_STORAGE: "local"
      ADMIN_USERNAME: "admin"
      ADMIN_PASSWORD: "change-me"
    volumes:
      - uploads:/app/uploads
      - db:/app

  client:
    image: ren4el/neco-board-client:latest
    ports:
      - "3000:80"
    environment:
      SERVER_URL: "http://localhost:8080"

volumes:
  uploads:
  db:
```

```bash
docker compose up -d
```

API will be available at `http://localhost:8080`.  
API documentation (Scalar UI) at `http://localhost:8080/scalar`.

---

### Run API Container Only

```bash
docker run -d \
  -p 8080:8080 \
  -e JWT_SECRET="your-secret-key-minimum-32-characters" \
  -e JWT_ISSUER="neco-board" \
  -e JWT_AUDIENCE="neco-board" \
  -e DATABASE_TYPE="sqlite" \
  -e FILE_STORAGE="local" \
  -e APP_ALLOW_ORIGINS="http://localhost:3000" \
  -v uploads:/app/uploads \
  ren4el/neco-board-api:latest
```

---

## Environment Variables

### Application

| Variable | Default | Description |
|----------|---------|-------------|
| `APP_PORT` | `8080` | Port the server listens on |
| `APP_HOST` | `*` | Host binding (`*` binds all interfaces) |
| `APP_ALLOW_ORIGINS` | — | Comma-separated list of allowed CORS origins (e.g. `http://localhost:3000,https://myapp.com`) |

### JWT Authentication

| Variable | Default | Description |
|----------|---------|-------------|
| `JWT_SECRET` | **required** | Signing key — minimum 32 characters |
| `JWT_ISSUER` | **required** | Token issuer claim |
| `JWT_AUDIENCE` | **required** | Token audience claim |
| `JWT_ACCESS_TTL` | `60` | Access token lifetime in minutes |
| `JWT_REFRESH_TTL` | `30` | Refresh token lifetime in days |

### Database

| Variable | Default | Description |
|----------|---------|-------------|
| `DATABASE_TYPE` | `sqlite` | Database backend — see options below |
| `DATABASE_HOST` | — | Database server hostname |
| `DATABASE_PORT` | — | Database server port |
| `DATABASE_NAME` | `neco-board-ce` | Database / schema name |
| `DATABASE_USER` | — | Database username |
| `DATABASE_PASSWORD` | — | Database password |

#### `DATABASE_TYPE` options

| Value | Description | Required variables |
|-------|-------------|-------------------|
| `sqlite` | File-based SQLite database, stored inside the container. No external server needed. Recommended for single-node or development setups. | *(none — host/port/user/pass are ignored)* |
| `postgres` | PostgreSQL. Recommended for production. Supports concurrent writes and horizontal scaling. | `DATABASE_HOST`, `DATABASE_PORT`, `DATABASE_NAME`, `DATABASE_USER`, `DATABASE_PASSWORD` |
| `mysql` | MySQL / MariaDB. | `DATABASE_HOST`, `DATABASE_PORT`, `DATABASE_NAME`, `DATABASE_USER`, `DATABASE_PASSWORD` |
| `mssql` | Microsoft SQL Server. | `DATABASE_HOST`, `DATABASE_PORT`, `DATABASE_NAME`, `DATABASE_USER`, `DATABASE_PASSWORD` |

> **Note:** When using `sqlite`, mount a volume to `/app` to persist the database file across container restarts.

**PostgreSQL example:**
```env
DATABASE_TYPE=postgres
DATABASE_HOST=postgres
DATABASE_PORT=5432
DATABASE_NAME=neco-board
DATABASE_USER=neco
DATABASE_PASSWORD=secret
```

### File Storage

| Variable | Default | Description |
|----------|---------|-------------|
| `FILE_STORAGE` | `local` | Storage backend — see options below |
| `LOCAL_STORAGE_PATH` | `/app/uploads` | Absolute path inside the container for local storage |
| `S3_STORAGE_REGION` | — | AWS region (e.g. `us-east-1`) |
| `S3_STORAGE_BUCKET` | — | S3 bucket name |
| `S3_STORAGE_ACCESS_KEY` | — | AWS access key ID |
| `S3_STORAGE_SECRET_KEY` | — | AWS secret access key |

#### `FILE_STORAGE` options

| Value | Description | Required variables |
|-------|-------------|-------------------|
| `local` | Files stored on the container filesystem at `LOCAL_STORAGE_PATH`. Mount a volume to that path to persist files across restarts. | *(none)* |
| `s3` | Files stored in an AWS S3 bucket (or any S3-compatible storage like MinIO). Suitable for production and multi-replica deployments. | `S3_STORAGE_REGION`, `S3_STORAGE_BUCKET`, `S3_STORAGE_ACCESS_KEY`, `S3_STORAGE_SECRET_KEY` |

**S3 example:**
```env
FILE_STORAGE=s3
S3_STORAGE_REGION=eu-central-1
S3_STORAGE_BUCKET=neco-board-uploads
S3_STORAGE_ACCESS_KEY=AKIAIOSFODNN7EXAMPLE
S3_STORAGE_SECRET_KEY=wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY
```

### Admin Account

These variables seed the initial administrator account on first startup. They are ignored if the admin account already exists.

| Variable | Default | Description |
|----------|---------|-------------|
| `ADMIN_USERNAME` | `admin` | Initial admin username |
| `ADMIN_PASSWORD` | `admin123` | Initial admin password — **change this in production** |

---

## Full Production Example (PostgreSQL + S3)

```yaml
services:
  api:
    image: ren4el/neco-board-api:latest
    ports:
      - "8080:8080"
    environment:
      APP_ALLOW_ORIGINS: "https://board.example.com"

      JWT_SECRET: "${JWT_SECRET}"
      JWT_ISSUER: "neco-board"
      JWT_AUDIENCE: "neco-board"
      JWT_ACCESS_TTL: "30"
      JWT_REFRESH_TTL: "7"

      DATABASE_TYPE: "postgres"
      DATABASE_HOST: "postgres"
      DATABASE_PORT: "5432"
      DATABASE_NAME: "neco-board"
      DATABASE_USER: "neco"
      DATABASE_PASSWORD: "${DB_PASSWORD}"

      FILE_STORAGE: "s3"
      S3_STORAGE_REGION: "eu-central-1"
      S3_STORAGE_BUCKET: "neco-board-uploads"
      S3_STORAGE_ACCESS_KEY: "${AWS_ACCESS_KEY}"
      S3_STORAGE_SECRET_KEY: "${AWS_SECRET_KEY}"

      ADMIN_USERNAME: "admin"
      ADMIN_PASSWORD: "${ADMIN_PASSWORD}"
    depends_on:
      postgres:
        condition: service_healthy
    restart: unless-stopped

  postgres:
    image: postgres:17-alpine
    environment:
      POSTGRES_DB: neco-board
      POSTGRES_USER: neco
      POSTGRES_PASSWORD: "${DB_PASSWORD}"
    volumes:
      - pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U neco -d neco-board"]
      interval: 5s
      timeout: 5s
      retries: 10
    restart: unless-stopped

  client:
    image: ren4el/neco-board-client:latest
    ports:
      - "3000:80"
    environment:
      SERVER_URL: "https://api.example.com"
    restart: unless-stopped

volumes:
  pgdata:
```

---

## Building from Source

```bash
git clone https://github.com/NecoCore/Neco-Board-CE-Mono.git
cd Neco-Board-CE-Mono

docker build -f neco-board-ce/Dockerfile -t neco-board-api .
```

---

## CI/CD Pipeline

The repository uses GitHub Actions to build and push the Docker image to Docker Hub automatically.

### Required GitHub Secrets

Go to **Settings → Secrets and variables → Actions** in your repository and add:

| Secret | Description |
|--------|-------------|
| `DOCKERHUB_USERNAME` | Your Docker Hub username (`ren4el`) |
| `DOCKERHUB_TOKEN` | A Docker Hub Access Token (create one at hub.docker.com → Account Settings → Personal Access Tokens) |

### Trigger Conditions

| Event | Action |
|-------|--------|
| Push to `main` or `master` | Build + push with `latest` and branch tags |
| Push a tag `v*.*.*` | Build + push with semver tags |
| Push a tag `beta-*` or `alpha-*` | Build + push with the pre-release tag as-is |
| Pull request to `main` / `master` | Image is built but **not pushed** |
| Manual (`workflow_dispatch`) | Build + push on demand |

### Available Image Tags

| Tag | Description |
|-----|-------------|
| `latest` | Latest stable build from the default branch |
| `beta-0.1` | Beta pre-release 0.1 |
| `v1.2.3` | Exact release version |
| `v1.2` | Latest patch of a minor version |
| `main` / `master` | Build from the named branch |
| `sha-<commit>` | Build tied to a specific commit |

### Releasing a Beta Version

To publish a `beta-0.1` image, create and push a Git tag:

```bash
git tag beta-0.1
git push origin beta-0.1
```

The pipeline will automatically build and push the image as:

```
ren4el/neco-board-api:beta-0.1
```

### Releasing a Stable Version

```bash
git tag v1.0.0
git push origin v1.0.0
```

This produces four tags: `v1.0.0`, `v1.0`, `v1`, and `latest`.

---

## License

Community Edition — see [LICENSE](LICENSE).
