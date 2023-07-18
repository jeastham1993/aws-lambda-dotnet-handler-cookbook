namespace SharedKernel.Features;

public interface IFeatureFlags
{
    public object Evaluate(
        string name,
        Dictionary<string, object> context = null,
        object defaultVal = null);

    public List<string> GetEnabledFeatures(Dictionary<string, object> context = null);
}