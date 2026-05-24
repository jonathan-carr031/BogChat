using System;
using SharpDX.Direct3D11;

namespace BogChatDesktopClient.Helpers;

class MultithreadLock : IDisposable
{
    private Multithread _multithread;

    public MultithreadLock(Multithread multithread)
    {
        _multithread = multithread;
        _multithread?.Enter();
    }

    public void Dispose()
    {
        _multithread?.Leave();
        _multithread = null;
    }
}