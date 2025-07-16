using ClassLibrary.Coordinates;

namespace ClassLibrary.Responses;

/// <summary>
/// Response from API
/// </summary>
public class Response
{
    public string Message { get; set; } = string.Empty;
    public CurrentPosition? CurrentPosition { get; set; }
    public List<AbsoluteObject> Objects { get; set; } = new();

}