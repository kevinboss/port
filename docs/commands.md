# Commands

Most commands accept an optional identifier or container name and open an
interactive picker when omitted. Every command except `mcp` also has a short
alias.

## config

```bash
port config [-o|--open]
```

Alias: `cfg`. Prints the path of the config file. Pass `-o` or `--open` to open
it in your default editor.

## list

```bash
port list [identifier]
```

Alias: `ls`. Shows every configured image, every tag, every snapshot, and the
running container if any. With an identifier, the listing is restricted to that
group.

## pull

```bash
port pull [identifier:tag]
```

Alias: `p`. Fetches the image. Per-layer download progress is shown live.

## run

```bash
port run [identifier:tag] [-r|--reset]
```

Alias: `r`. Launches a container. Any container holding the same host port is
stopped first. `-r` (`--reset`) recreates the container instead of restarting
an existing one. Pulls the image if it is not local yet.

## stop

```bash
port stop [containerName]
```

Alias: `s`. Stops a running container by its full name (for example
`Nginx.alpine`). With no name, port shows a picker.

## reset

```bash
port reset [containerName]
```

Alias: `rs`. Stops, removes, and recreates a running container to discard
mutations made inside it. With no name, port selects the running container
automatically if only one is running, or shows a picker.

## commit

```bash
port commit [containerName] [-t|--tag <tag>] [-s|--switch] [-o|--overwrite]
```

Alias: `c`. Snapshots a running container into a new image. `-t` (`--tag`) sets
the tag and defaults to a timestamp (`yyyyMMddhhmmss`). `-s` (`--switch`) stops
the source container and switches to the new snapshot. `-o` (`--overwrite`)
overwrites the source tag and relaunches from the updated image.

## remove

```bash
port remove [identifier:tag] [-r|--recursive]
```

Alias: `rm`. Removes the image and any containers using it. `-r`
(`--recursive`) also removes descendant snapshot images.

## prune

```bash
port prune [identifier]
```

Alias: `pr`. Removes dangling (digest-only) images not pinned in the config.
Restrict to one identifier with the optional argument.

## mcp

```bash
port mcp
```

Starts the Model Context Protocol server over stdio. See [mcp.md](mcp.md).
