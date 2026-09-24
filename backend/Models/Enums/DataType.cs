namespace backend.Models.Enums;

/// <summary>
/// Supported data types for Attribute Definitions (Spec §4.3).
/// </summary>
public enum DataType
{
    String = 1,
    Text = 2,
    Image = 3,
    Numeric = 4,
    Date = 5,
    Period = 6,
    Boolean = 7,
    OneOfMany = 8
}
