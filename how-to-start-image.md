# How to start the app with Podman

## 1. Prerequisites — once only

```powershell
# Check that the Podman machine is running
podman machine list

# If stopped:
podman machine start

# Install podman-compose (requires Python)
pip install podman-compose
```

---

## 2. Prepare secrets

```powershell
copy .env.example .env
```

Edit `.env` with your values. Required at minimum:

| Variable | Description |
|---|---|
| `MYSQL_ROOT_PASSWORD` | MySQL root password |
| `MYSQL_PASSWORD` | App DB user password |
| `JWT_SECRET_KEY` | Min 32 chars random string |
| `HTTP_PORT` | Port exposed by nginx (default `8080`) |

---

## 3. Build the Angular frontend

```powershell
cd client
npm run build
cd ..
```

---

## 4. Build the API image

> Podman Desktop UI has a bug with multi-stage builds when `dockerfile:` is set explicitly.
> Always build the API image manually via CLI first.

```powershell
podman build -t expatriate365-api -f Dockerfile .
```

---

## 5. Start everything

```powershell
# Start all services (uses the image built above, no --build needed)
podman compose up -d

# Watch API logs (EF Core migrations run at startup)
podman compose logs -f api
```

---

## 6. Daily commands

```powershell
# Status of all containers
podman compose ps

# Logs
podman compose logs -f api
podman compose logs -f frontend

# Stop without deleting volumes (MySQL data preserved)
podman compose down

# Stop AND delete volumes (full reset)
podman compose down -v

# Rebuild the API image after a code change, then restart
podman build -t expatriate365-api -f Dockerfile .
podman compose up -d
```

---

## 7. Access

| Service | URL |
|---|---|
| Angular app | `http://localhost:8080` |
| Scalar API docs | `http://localhost:8080/scalar/` |
