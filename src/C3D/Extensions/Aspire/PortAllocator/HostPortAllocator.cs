namespace C3D.Extensions.Aspire;

public class HostPortAllocator : global::Aspire.Hosting.ApplicationModel.IPortAllocator
{
    private readonly Networking.PortAllocator portAllocator;
    private readonly int startPort;

    public HostPortAllocator(C3D.Extensions.Networking.PortAllocator portAllocator, int startPort = 8000)
    {
        this.portAllocator = portAllocator;
        this.startPort = startPort;
    }

    public void AddUsedPort(int port) => portAllocator.MarkPortAsUsed(port);

    public int AllocatePort() => portAllocator.GetRandomFreePort(startPort);
}
