# Configuration

Port reads `~/.port` on Linux and macOS, or `%USERPROFILE%\.port` on Windows.
If the file does not exist, a default one is written on first run with a
`Getting.Started` image entry and a platform-appropriate `dockerEndpoint`
(`npipe://./pipe/docker_engine` on Windows, `unix:///var/run/docker.sock` on
Linux and macOS).

If an older `~/.dcma` file exists and `~/.port` does not, port renames it to
`~/.port`. Config files with `version: 1.0` are migrated to `1.1` on load.

## Config file format

```yaml
version: 1.1
dockerEndpoint: unix:///var/run/docker.sock
imageConfigs:
  - identifier: Nginx
    imageName: nginx
    imageTags:
      - alpine
      - alpine-slim
    ports:
      - 8080:80
    environment: []
  - identifier: Alpine
    imageName: alpine
    imageTags:
      - '3.19'
      - latest
    ports: []
    environment: []
```

`version` is the config schema version (`1.1`).

`dockerEndpoint` is the Docker daemon URI (for example
`unix:///var/run/docker.sock` or `npipe://./pipe/docker_engine`).

`identifier` is the human-friendly name port uses everywhere (commands,
container names, and snapshot tags).

`imageName` is the actual Docker image name.

`imageTags` is the list of tags port knows how to launch.

`ports` is a list of `host:container` port mappings.

`environment` is a list of `KEY=VALUE` environment variables passed to the
container.

## Docker Compose

You can drop a Docker Compose file next to where you invoke port and the
services in it are merged in. Port checks the current working directory for
`docker-compose.yml`, `docker-compose.yaml`, `compose.yml`, and `compose.yaml`
in that order.

Each service that specifies `image` is converted into an image config entry
using the service name as its `identifier`, along with its `ports` and
`environment`. Services from the Compose file override any global `~/.port`
entry with the same identifier.

## PowerShell Unicode

To get proper rendering of port's output in PowerShell, add this to your
`$profile`:

```powershell
[console]::InputEncoding = [console]::OutputEncoding = [System.Text.UTF8Encoding]::new()
```
