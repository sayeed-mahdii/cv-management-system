namespace backend.Models.Enums;

/// <summary>
/// Comparison operators for Position Access Rules (Spec §4.4 and Database Design §5).
/// Operators vary based on the Attribute DataType.
/// </summary>
public enum AccessRuleOperator
{
    // Numeric & Text
    Equals = 1,
    NotEquals = 2,

    // Numeric
    GreaterThan = 3,
    GreaterOrEqual = 4,
    LessThan = 5,
    LessOrEqual = 6,

    // String / Text
    Contains = 7,

    // Boolean
    IsChecked = 8,
    IsNotChecked = 9,

    // Date
    Before = 10,
    After = 11
}
