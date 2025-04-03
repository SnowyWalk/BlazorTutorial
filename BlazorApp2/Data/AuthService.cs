using BlazorApp2.Attributes;
using BlazorApp2.Util;
namespace BlazorApp2.Data;

public class AuthService : PersistableClass
{
    public bool IsLoggedIn => CurrentUser.Username.Length != 0;
    
    [Persistable]
    public User CurrentUser { get; set; } = new User();
}