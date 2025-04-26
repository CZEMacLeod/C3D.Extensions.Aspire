namespace C3D.Extensions.Aspire.IISExpress.Annotations;

public class SysTrayArgumentAnnotation(bool showInSysTray) : IISExpressArgumentAnnotation
{
    public bool ShowInSysTray => showInSysTray;

    public override string ToString() => $"/systray:{showInSysTray}";
}