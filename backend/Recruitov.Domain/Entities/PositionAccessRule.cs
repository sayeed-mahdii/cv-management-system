using Recruitov.Domain.Enums;

namespace Recruitov.Domain.Entities;

/// <summary>
/// Candidate access restriction rule for a Position.
/// Example: IELTS Score (Attribute) GreaterThan (Operator) "7.0" (TargetValue).
/// </summary>
public class PositionAccessRule
{
    public int Id { get; set; }

    public int PositionId { get; set; }

    public int AttributeDefinitionId { get; set; }

    public AccessRuleOperator Operator { get; set; }

    /// <summary>
    /// Target value in canonical string format.
    /// </summary>
    public string TargetValue { get; set; } = string.Empty;

    // Navigation properties
    public Position? Position { get; set; }
    public AttributeDefinition? AttributeDefinition { get; set; }
}
