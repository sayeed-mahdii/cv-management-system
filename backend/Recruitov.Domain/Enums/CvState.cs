namespace Recruitov.Domain.Enums;

/// <summary>
/// State tracking for Virtual CVs.
/// Draft: Work in progress, editable by Candidate.
/// Published: All mandatory fields completed, visible to Recruiters.
/// </summary>
public enum CvState
{
    Draft = 1,
    Published = 2
}
