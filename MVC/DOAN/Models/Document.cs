using System.IO.Pipelines;

public class Document
{
    public int Id {get ; set;}
    public string FileName {get; set;} = string.Empty;
    public string FilePath {get; set; } = string.Empty;
    public string UpLoadedBy {get; set; } = string.Empty;
    public DateTime UpLoadDate {get; set; } = DateTime.UtcNow;
    
}