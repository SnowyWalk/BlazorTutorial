using BlazorApp2.Attributes;
namespace BlazorApp2.Data;

public class User
{
    [Persistable]
    public string Username { get; set; } = string.Empty;
    [Persistable]
    public string ProfileImageUrl { get; set; } = string.Empty;
}