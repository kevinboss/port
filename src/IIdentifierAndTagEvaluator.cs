namespace dcma;

public interface IIdentifierAndTagEvaluator
{
    (string identifier, string tag) Evaluate(string imageIdentifier);
}