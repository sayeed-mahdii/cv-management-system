namespace Recruitov.Application.Dtos;

/// <summary>
/// Aggregated platform metrics displayed on the main page.
/// </summary>
public class PlatformStatsDto
{
    public int TotalPositions { get; set; }
    public int TotalApplications { get; set; }
    public int TotalCandidates { get; set; }
    public int TotalRecruiters { get; set; }
    public int ApplicationsLast24Hours { get; set; }
}

/// <summary>
/// Technology tag frequency item for react-tagcloud.
/// </summary>
public class TagCloudItemDto
{
    public string Value { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>
/// Single round-trip dashboard payload returning metrics, latest vacancies,
/// popular positions, and technology tags (zero N+1 queries).
/// </summary>
public class DashboardResponseDto
{
    public PlatformStatsDto Stats { get; set; } = new();
    public List<PositionSummaryDto> LatestPositions { get; set; } = new();
    public List<PositionSummaryDto> PopularPositions { get; set; } = new();
    public List<TagCloudItemDto> TagCloud { get; set; } = new();
}
