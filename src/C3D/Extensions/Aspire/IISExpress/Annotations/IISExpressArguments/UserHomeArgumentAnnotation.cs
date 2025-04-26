namespace C3D.Extensions.Aspire.IISExpress.Annotations;

public class UserHomeArgumentAnnotation(string userHome) : IISExpressArgumentAnnotation
{
    public string UserHome => userHome;

    public const string DefaultUserHome = "%userprofile%\\documents\\iisexpress";

    public override string ToString() => $"/userhome:{userHome}";
}
