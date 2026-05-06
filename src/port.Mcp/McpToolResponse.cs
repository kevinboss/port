namespace port.Mcp;

public sealed record McpToolResponse<T>(T Result, IReadOnlyList<string> Events);
