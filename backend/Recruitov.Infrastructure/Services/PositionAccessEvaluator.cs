using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Recruitov.Application.Interfaces;
using Recruitov.Domain.Enums;
using Recruitov.Infrastructure.Data;

namespace Recruitov.Infrastructure.Services;

public class PositionAccessEvaluator(
    AppDbContext db,
    ILogger<PositionAccessEvaluator> logger) : IPositionAccessEvaluator
{
    public async Task<AccessEvaluationResult> EvaluateAsync(
        int positionId,
        string candidateUserId,
        CancellationToken ct = default)
    {
        var position = await db.Positions
            .AsNoTracking()
            .Where(p => p.Id == positionId)
            .Select(p => new
            {
                p.IsPublic,
                Rules = p.PositionAccessRules.Select(r => new
                {
                    r.AttributeDefinitionId,
                    AttributeName = r.AttributeDefinition!.Name,
                    DataType = r.AttributeDefinition.DataType,
                    r.Operator,
                    r.TargetValue
                }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (position is null || position.IsPublic || position.Rules.Count == 0)
            return AccessEvaluationResult.Allow();

        var requiredAttrIds = position.Rules
            .Select(r => r.AttributeDefinitionId)
            .Distinct()
            .ToList();

        var candidateValues = await db.UserAttributeValues
            .AsNoTracking()
            .Where(v => v.UserId == candidateUserId && requiredAttrIds.Contains(v.AttributeDefinitionId))
            .ToDictionaryAsync(v => v.AttributeDefinitionId, v => v.Value, ct);

        var failedRules = new List<FailedAccessRuleDto>();

        foreach (var rule in position.Rules)
        {
            candidateValues.TryGetValue(rule.AttributeDefinitionId, out var actualRaw);
            var actual = actualRaw?.Trim() ?? string.Empty;

            if (!IsRuleSatisfied(rule.DataType, rule.Operator, actual, rule.TargetValue))
            {
                failedRules.Add(new FailedAccessRuleDto(
                    rule.AttributeDefinitionId,
                    rule.AttributeName,
                    $"{rule.Operator} {rule.TargetValue}",
                    string.IsNullOrEmpty(actual) ? "(missing)" : actual));
            }
        }

        if (failedRules.Count > 0)
        {
            logger.LogInformation(
                "Candidate {UserId} failed {Count} access rule(s) for Position {PositionId}",
                candidateUserId, failedRules.Count, positionId);
            return AccessEvaluationResult.Deny(failedRules);
        }

        return AccessEvaluationResult.Allow();
    }

    public static bool IsRuleSatisfied(DataType dataType, AccessRuleOperator op, string actual, string target) =>
        dataType switch
        {
            DataType.Numeric => EvaluateNumeric(op, actual, target),
            DataType.Boolean => EvaluateBoolean(op, actual),
            DataType.Date => EvaluateDate(op, actual, target),
            _ => EvaluateText(op, actual, target)
        };

    private static bool EvaluateNumeric(AccessRuleOperator op, string actual, string target)
    {
        if (!decimal.TryParse(actual, NumberStyles.Number, CultureInfo.InvariantCulture, out var a) ||
            !decimal.TryParse(target, NumberStyles.Number, CultureInfo.InvariantCulture, out var t))
            return false;

        return op switch
        {
            AccessRuleOperator.Equals => a == t,
            AccessRuleOperator.NotEquals => a != t,
            AccessRuleOperator.GreaterThan => a > t,
            AccessRuleOperator.GreaterOrEqual => a >= t,
            AccessRuleOperator.LessThan => a < t,
            AccessRuleOperator.LessOrEqual => a <= t,
            _ => false
        };
    }

    private static bool EvaluateBoolean(AccessRuleOperator op, string actual)
    {
        var isTrue = bool.TryParse(actual, out var parsed) && parsed;
        return op switch
        {
            AccessRuleOperator.IsChecked => isTrue,
            AccessRuleOperator.IsNotChecked => !isTrue,
            _ => false
        };
    }

    private static bool EvaluateDate(AccessRuleOperator op, string actual, string target)
    {
        if (!DateOnly.TryParse(actual, CultureInfo.InvariantCulture, DateTimeStyles.None, out var a) ||
            !DateOnly.TryParse(target, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
            return false;

        return op switch
        {
            AccessRuleOperator.Before => a < t,
            AccessRuleOperator.After => a > t,
            AccessRuleOperator.Equals => a == t,
            _ => false
        };
    }

    private static bool EvaluateText(AccessRuleOperator op, string actual, string target) => op switch
    {
        AccessRuleOperator.Equals => actual.Equals(target, StringComparison.OrdinalIgnoreCase),
        AccessRuleOperator.NotEquals => !actual.Equals(target, StringComparison.OrdinalIgnoreCase),
        AccessRuleOperator.Contains => actual.Contains(target, StringComparison.OrdinalIgnoreCase),
        _ => false
    };
}
