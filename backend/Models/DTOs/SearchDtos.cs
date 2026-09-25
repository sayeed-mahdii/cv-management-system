namespace backend.Models.DTOs;

public class SearchResultItemDto
{
    public string Id { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "Position", "Project", "Candidate"
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string Url { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}

public class GlobalSearchResponseDto
{
    public string Query { get; set; } = string.Empty;
    public int TotalResults { get; set; }
    public List<SearchResultItemDto> Results { get; set; } = new();
}
