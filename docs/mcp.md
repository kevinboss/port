# MCP server

`port mcp` starts a [Model Context Protocol](https://modelcontextprotocol.io)
server over stdio. AI agents can then drive port the same way you do from the
shell.

## Tools

The exposed tools mirror the CLI verbs:

`run(identifier, tag, reset=false)` launches a configured container and stops
any container using the same host ports first. Both `identifier` and `tag` are
required.

`stop(containerName)` stops a running container by its exact container name.

`reset(containerName)` recreates a running container from its image.
`containerName` is always required (no auto-pick).

`commit(containerName, tag, overwrite=false, switch=false)` creates a snapshot
image from a running container. `tag` is required (no timestamp default).

`pull(identifier, tag=null)` pulls a configured image and returns a summary
rather than per-layer events.

`remove(identifier, tag=null, recursive=false)` removes an image and its
dependent containers. Pass `recursive=true` to remove descendant snapshots.

`prune(identifier=null)` removes dangling (digest-only) images, optionally
restricted to one identifier.

`list(identifier=null)` lists configured images, tags, snapshots, dangling
images, and running containers.

`config()` returns the absolute path to port's config file.

A few tools have stricter inputs than their CLI siblings (`reset` always
requires a container name, `commit` requires an explicit tag, and `run`
requires both `identifier` and `tag`) to remove ambiguity for an agent.

## Claude Code

```bash
claude mcp add port -- port mcp
```

## Claude Desktop

Add to your `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "port": {
      "command": "port",
      "args": ["mcp"]
    }
  }
}
```
