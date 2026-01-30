using System;
using System.Collections.Generic;
using System.Text;

namespace SWATestProject.Fixtures;

public class NetworkingFixture
{
    public NetworkingFixture()
    {
        this.PortAllocator = new C3D.Extensions.Networking.PortAllocator();
    }

    public C3D.Extensions.Networking.PortAllocator PortAllocator { get; }
}
