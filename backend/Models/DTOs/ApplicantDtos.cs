namespace backend.Models.DTOs;

/// <summary>
/// Summary of an applicant displayed in the recruiter's applicant table for a position.
/// </summary>
public class ApplicantSummaryDto
{
    public int CvId { get; set; }
    public string CandidateId { get; set; } = string.Empty;
    public string CandidateName { get; set; } = string.Empty;
    public string CandidateEmail { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string State { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public int Version { get; set; }
}

/// <summary>
/// Status check of how well an applicant satisfies a required attribute for a position.
/// </summary>
public class ApplicantAttributeMatchDto
{
    public int AttributeDefinitionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string CandidateValue { get; set; } = string.Empty;
    public bool IsSatisfied { get; set; }
}

/// <summary>
/// Full Virtual CV Inspector view for recruiters (Killer Feature #3).
/// Includes candidate's identity, dynamic attributes check, and tailored projects
/// filtered by position tags and capped by MaxProjects.
/// </summary>
public class ApplicantDetailDto
{
    public int CvId { get; set; }
    public int PositionId { get; set; }
    public string PositionTitle { get; set; } = string.Empty;
    public int MaxProjects { get; set; }
    public List<string> PositionTags { get; set; } = new();

    public string CandidateId { get; set; } = string.Empty;
    public string CandidateEmail { get; set; } = string.Empty;
    public string CandidateName { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string State { get; set; } = string.Empty;

    public List<UserAttributeValueDto> MeAttributes { get; set; } = new();
    public List<UserAttributeValueDto> InfoAttributes { get; set; } = new();
    public List<ApplicantAttributeMatchDto> RequiredAttributesCheck { get; set; } = new();
    public List<ProjectDto> Projects { get; set; } = new();
}
