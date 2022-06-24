namespace port;

public interface IImageIdentifierAndTagEvaluator
{
    (string identifier, string tag) Evaluate(string imageIdentifier);
}