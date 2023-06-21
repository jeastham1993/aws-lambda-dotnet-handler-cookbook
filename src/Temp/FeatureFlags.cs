namespace Temp;

using System.Text.Json;

public class FeatureFlags
{
    private readonly Dictionary<string, object> features;

    public FeatureFlags(Dictionary<string, object> features)
    {
        this.features = features;
    }

    private delegate bool ActionDelegate(object a, object b);

    private bool MatchByAction(string action, object conditionValue, object contextValue)
    {
        Dictionary<string, ActionDelegate> mappingByAction = new Dictionary<string, ActionDelegate>
        {
            { RuleAction.EQUALS, (a, b) => a.Equals(b) },
            { RuleAction.NOT_EQUALS, (a, b) => !a.Equals(b) },
            { RuleAction.KEY_GREATER_THAN_VALUE, (a, b) => Convert.ToDecimal(a) > Convert.ToDecimal(b) },
            { RuleAction.KEY_GREATER_THAN_OR_EQUAL_VALUE, (a, b) => Convert.ToDecimal(a) >= Convert.ToDecimal(b) },
            { RuleAction.KEY_LESS_THAN_VALUE, (a, b) => Convert.ToDecimal(a) < Convert.ToDecimal(b) },
            { RuleAction.KEY_LESS_THAN_OR_EQUAL_VALUE, (a, b) => Convert.ToDecimal(a) <= Convert.ToDecimal(b) },
            { RuleAction.STARTSWITH, (a, b) => a.ToString().StartsWith(b.ToString()) },
            { RuleAction.ENDSWITH, (a, b) => a.ToString().EndsWith(b.ToString()) },
            { RuleAction.IN, (a, b) => ((IEnumerable<object>)b).Contains(a) },
            { RuleAction.NOT_IN, (a, b) => !((IEnumerable<object>)b).Contains(a) },
            { RuleAction.KEY_IN_VALUE, (a, b) => ((IEnumerable<object>)b).Contains(a) },
            { RuleAction.KEY_NOT_IN_VALUE, (a, b) => !((IEnumerable<object>)b).Contains(a) },
            { RuleAction.VALUE_IN_KEY, (a, b) => ((IEnumerable<object>)a).Contains(b) },
            { RuleAction.VALUE_NOT_IN_KEY, (a, b) => !((IEnumerable<object>)a).Contains(b) }
        };

        try
        {
            ActionDelegate func = mappingByAction.GetValueOrDefault(action, (a, b) => false);
            return func(conditionValue, contextValue);
        }
        catch (Exception exc)
        {
            Console.WriteLine($"caught exception while matching action: action={action}, exception={exc.ToString()}");
            return false;
        }
    }

    private bool EvaluateConditions(string ruleName, string featureName, Dictionary<string, object> rule, Dictionary<string, object> context)
    {
        object ruleMatchValue = rule.GetValueOrDefault(Schema.RULE_MATCH_VALUE);

        var conditionsVal = rule.GetValueOrDefault(Schema.CONDITIONS_KEY);

        List<Dictionary<string, object>> conditions = conditionsVal == null
            ? null
            : JsonSerializer.Deserialize<List<Dictionary<string, object>>>(conditionsVal.ToString());

        if (conditions == null || conditions.Count == 0)
        {
            Console.WriteLine($"rule did not match, no conditions to match, rule_name={ruleName}, rule_value={ruleMatchValue}, name={featureName}");
            return false;
        }

        foreach (var condition in conditions)
        {
            string contextValue = context.GetValueOrDefault(condition.GetValueOrDefault(Schema.CONDITION_KEY, "").ToString()).ToString();
            string condAction = condition.GetValueOrDefault(Schema.CONDITION_ACTION, "").ToString();
            string condValue = condition.GetValueOrDefault(Schema.CONDITION_VALUE).ToString();

            if (!MatchByAction(condAction, condValue, contextValue))
            {
                Console.WriteLine($"rule did not match action, rule_name={ruleName}, rule_value={ruleMatchValue}, name={featureName}, context_value={contextValue}");
                return false; // context doesn't match condition
            }
        }

        Console.WriteLine($"rule matched, rule_name={ruleName}, rule_value={ruleMatchValue}, name={featureName}");
        return true;
    }

    private object EvaluateRules(string featureName, Dictionary<string, object> context, object featDefault, Dictionary<string, object> rules, bool booleanFeature)
    {
        foreach (var ruleEntry in rules)
        {
            string ruleName = ruleEntry.Key;
            Dictionary<string, object> rule = JsonSerializer.Deserialize<Dictionary<string, object>>(ruleEntry.Value.ToString());
            object ruleMatchValue = rule.GetValueOrDefault(Schema.RULE_MATCH_VALUE);

            // Context might contain PII data; do not log its value
            Console.WriteLine($"Evaluating rule matching, rule={ruleName}, feature={featureName}, default={featDefault}, boolean_feature={booleanFeature}");

            if (EvaluateConditions(ruleName, featureName, rule, context))
            {
                // Maintenance: Revisit before going GA.
                return booleanFeature ? Convert.ToBoolean(ruleMatchValue.ToString()) : ruleMatchValue.ToString();
            }
        }

        // no rule matched, return default value of feature
        Console.WriteLine($"no rule matched, returning feature default, default={featDefault}, name={featureName}, boolean_feature={booleanFeature}");
        return booleanFeature ? Convert.ToBoolean(featDefault.ToString()) : featDefault.ToString();
    }

    public object Evaluate(string name, Dictionary<string, object> context = null, object defaultVal = null)
    {
        context ??= new Dictionary<string, object>();

        var featureVal = features.GetValueOrDefault(name);

        var feature = featureVal == null ? null : JsonSerializer.Deserialize<Dictionary<string, Object>>(featureVal.ToString());
        
        if (feature == null)
        {
            Console.WriteLine($"Feature not found; returning default provided, name={name}, default={defaultVal}");
            return defaultVal;
        }

        var ruleVal = feature.GetValueOrDefault(Schema.RULES_KEY);

        var rules = ruleVal == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(ruleVal.ToString());
        var featDefault = feature.GetValueOrDefault(Schema.FEATURE_DEFAULT_VAL_KEY);
        // Maintenance: Revisit before going GA. We might to simplify customers on-boarding by not requiring it
        // for non-boolean flags. It'll need minor implementation changes, docs changes, and maybe refactor
        // get_enabled_features. We can minimize breaking change, despite Beta label, by having a new
        // method `get_matching_features` returning Dict[feature_name, feature_value]
        var booleanFeature = Convert.ToBoolean(feature.GetValueOrDefault(Schema.FEATURE_DEFAULT_VAL_TYPE_KEY, true)); // backwards compatibility, assume feature flag
        if (rules == null)
        {
            Console.WriteLine($"no rules found, returning feature default, name={name}, default={featDefault}, boolean_feature={booleanFeature}");
            // Maintenance: Revisit before going GA. We might to simplify customers on-boarding by not requiring it
            // for non-boolean flags.
            return booleanFeature ? Convert.ToBoolean(featDefault) : featDefault;
        }

        Console.WriteLine($"looking for rule match, name={name}, default={featDefault}, boolean_feature={booleanFeature}");
        return EvaluateRules(name, context, featDefault, rules, booleanFeature);
    }

    public List<string> GetEnabledFeatures(Dictionary<string, object> context = null)
    {
        context ??= new Dictionary<string, object>();
        List<string> featuresEnabled = new List<string>();

        Console.WriteLine("Evaluating all features");
        foreach (var featureEntry in features)
        {
            string name = featureEntry.Key;
            Dictionary<string, object> feature =
                JsonSerializer.Deserialize<Dictionary<string, object>>(featureEntry.Value.ToString());

            var val = feature.GetValueOrDefault(Schema.RULES_KEY);
            
            Dictionary<string, object> rules = val == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(val.ToString());
            var featureDefaultVal = feature.GetValueOrDefault(Schema.FEATURE_DEFAULT_VAL_KEY);
            var booleanFeature = Convert.ToBoolean(feature.GetValueOrDefault(Schema.FEATURE_DEFAULT_VAL_TYPE_KEY, true)); // backwards compatibility, assume feature flag

            if (featureDefaultVal != null && rules == null)
            {
                Console.WriteLine($"feature is enabled by default and has no defined rules, name={name}");
                featuresEnabled.Add(name);
            }
            else if (EvaluateRules(name, context, featureDefaultVal, rules, booleanFeature) != null)
            {
                Console.WriteLine($"feature's calculated value is True, name={name}");
                featuresEnabled.Add(name);
            }
        }

        return featuresEnabled;
    }
}

public class Schema
{
    public const string RULE_MATCH_VALUE = "when_match";
    public const string CONDITIONS_KEY = "conditions";
    public const string CONDITION_KEY = "key";
    public const string CONDITION_ACTION = "action";
    public const string CONDITION_VALUE = "value";
    public const string RULES_KEY = "rules";
    public const string FEATURE_DEFAULT_VAL_KEY = "default";
    public const string FEATURE_DEFAULT_VAL_TYPE_KEY = "default_value_type";
}

public static class RuleAction
{
    public const string EQUALS = "EQUALS";
    public const string NOT_EQUALS = "NOT_EQUALS";
    public const string KEY_GREATER_THAN_VALUE = "KEY_GREATER_THAN_VALUE";
    public const string KEY_GREATER_THAN_OR_EQUAL_VALUE = "KEY_GREATER_THAN_OR_EQUAL_VALUE";
    public const string KEY_LESS_THAN_VALUE = "KEY_LESS_THAN_VALUE";
    public const string KEY_LESS_THAN_OR_EQUAL_VALUE = "KEY_LESS_THAN_OR_EQUAL_VALUE";
    public const string STARTSWITH = "STARTSWITH";
    public const string ENDSWITH = "ENDSWITH";
    public const string IN = "IN";
    public const string NOT_IN = "NOT_IN";
    public const string KEY_IN_VALUE = "KEY_IN_VALUE";
    public const string KEY_NOT_IN_VALUE = "KEY_NOT_IN_VALUE";
    public const string VALUE_IN_KEY = "VALUE_IN_KEY";
    public const string VALUE_NOT_IN_KEY = "VALUE_NOT_IN_KEY";
    public const string SCHEDULE_BETWEEN_TIME_RANGE = "SCHEDULE_BETWEEN_TIME_RANGE";
    public const string SCHEDULE_BETWEEN_DATETIME_RANGE = "SCHEDULE_BETWEEN_DATETIME_RANGE";
    public const string SCHEDULE_BETWEEN_DAYS_OF_WEEK = "SCHEDULE_BETWEEN_DAYS_OF_WEEK";
    public const string MODULO_RANGE = "MODULO_RANGE";
}