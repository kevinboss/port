namespace port;

public interface IContainerNamePrompt
{
    string GetIdentifierOfContainerFromUser(
        IReadOnlyCollection<Container> readOnlyCollection,
        string command
    );
}
