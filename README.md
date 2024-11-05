# Docker Event Logger

This app logs docker events to a MySQL database.

To use it, set MySQLConnectionString in the environment variables and bind /var/run/docker.sock readonly, like so:

```
services:
  docker-events:
    build: .
    environment:
      MySQLConnectionString: ${DOCKER_EVENT_LOGGER_MYSQL_CONNECTION_STRING}
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock:ro

```
