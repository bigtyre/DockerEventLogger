# Docker Event Logger

This app logs docker events to a MySQL database.

Before running the app, copy `example.env` to `.env` in the solution root:

```powershell
Copy-Item example.env .env
```

Edit `.env` and replace the placeholders in `MySqlConnectionString` with your MySQL host, database name, username, and password.

Then start the app using Docker Compose:

```sh
docker compose up --build -d
```

The included Compose configuration loads `.env` into the container and mounts `/var/run/docker.sock` read-only so the app can monitor Docker events.
