# C3D.Extensions.Aspire.PortAllocator

This uses the `C3D.Extensions.Networking.PortAllocator` package to implement an aspire compatible `IPortAllocator`.
This version has a global list of allocated ports, and will scan the host system to check for any allocated port before starting.
It is useful to ensure that if you are allocating custom/random ports for services while testing, that you don't break because there is something else using that port.

It can be useful in integration testing to ensure that each test stack uses different ports so the tests don't conflict with one another.

Please see [C3D.Extensions.Networking.PortAllocator](https://github.com/CZEMacLeod/C3D.Extensions.Networking.PortAllocator) for more information.
