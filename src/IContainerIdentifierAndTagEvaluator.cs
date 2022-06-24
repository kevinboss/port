namespace port;

public interface IContainerIdentifierAndTagEvaluator
{
    (string identifier, string tag) Evaluate(string imageIdentifier);
}