namespace port;

public interface IIdentifierAndTagEvaluator
{
    (string identifier, string tag) Evaluate(string imageIdentifier);
}