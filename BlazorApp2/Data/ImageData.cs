using BlazorApp2.Attributes;
namespace BlazorApp2.Data;

public class ImageData
{
    [Persistable]
    public Guid GUID { get; set; }
    
    [Persistable]
    public string GroupKey { get; set; }
    
    [Persistable]
    public string FileName { get; set; }
    
    [Persistable]
    public string RelativePath { get; set; }

    [Persistable]
    public DateTime CreateDateTime { get; set; }

    public ImageData()
    {
        GUID = Guid.NewGuid();
    }
}