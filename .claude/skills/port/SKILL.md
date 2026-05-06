---
name: port
description: Run port CLI commands to manage configured Docker images and containers (run, stop, reset, commit, pull, remove, prune, list, config)
user-invocable: true
---

# port CLI Skill

Run port via `dotnet run --project src/port --` (debug build). For an installed
binary, drop the prefix and use `port` directly.

## Configuration

port reads `~/.port` (or `%USERPROFILE%\.port` on Windows). Each entry has an
`identifier`, an `imageName`, a list of `imageTags`, plus optional `ports` and
`environment`. The identifier is the human-friendly name port uses everywhere;
the imageName is the actual Docker image. A `docker-compose.yml` next to the
working directory is merged in if present.

Run `dotnet run --project src/port -- config` to print the path; pass `--open`
to open it in the editor.

## Commands

### list — show configured images, tags, snapshots, and containers
```bash
dotnet run --project src/port -- list [identifier]
```
With an identifier, restricts to that group. Each row shows the running state
(■ green = running, red = stopped/not pulled), creation timestamp, and the
parent tag for snapshots.

### pull — fetch an image
```bash
dotnet run --project src/port -- pull <identifier:tag>
```
Per-layer download progress is shown live.

### run — launch a configured image as a container
```bash
dotnet run --project src/port -- run <identifier:tag> [-r]
```
- Stops any container holding the same host port first.
- `-r` recreates the container instead of restarting an existing one.
- Pulls the image if it is not already local.

### stop — stop a running container
```bash
dotnet run --project src/port -- stop [<containerName>]
```
Container name is the full `Identifier.tag` form (e.g. `Nginx.alpine`). With no
name, port shows a picker of running containers.

### reset — recreate a running container from its image
```bash
dotnet run --project src/port -- reset [<containerName>]
```
Discards in-container state. The CLI auto-picks if exactly one container is
running; otherwise it shows a picker.

### commit — snapshot a running container into a new image
```bash
dotnet run --project src/port -- commit [<containerName>] [-t tag] [-s] [-o]
```
- `-t` sets the snapshot tag (defaults to a timestamp).
- `-s` stops the source and switches to the new snapshot.
- `-o` overwrites the source tag.

### remove — delete an image and any containers using it
```bash
dotnet run --project src/port -- remove <identifier:tag> [-r]
```
- `-r` also removes descendant snapshot images.

### prune — delete dangling (digest-only) images
```bash
dotnet run --project src/port -- prune [identifier]
```
Optionally restrict to one identifier.

### mcp — start the MCP server over stdio
```bash
dotnet run --project src/port -- mcp
```
Used by AI agents (Claude Desktop, Claude Code) — not meant for direct
terminal use. The MCP tools mirror the CLI verbs but with stricter inputs:
`reset` always requires a container name and `commit` always requires a tag.

## Notes

- An identifier is the configured logical name; a tag is the variant; a
  container name is the full `Identifier.tag` joining of the two.
- All commands that change Docker state (`run`, `stop`, `reset`, `commit`,
  `remove`, `prune`) re-render the list at the end.
- The CLI uses Spectre.Console — interactive prompts and live progress widgets
  do not pipe well. Use `--json`-style consumption only via the MCP server.
