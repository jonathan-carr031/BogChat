using System;
using System.Runtime.InteropServices;
using Windows.Graphics;
using Windows.Graphics.DirectX.Direct3D11;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using Device = SharpDX.Direct3D11.Device;
using Device3 = SharpDX.DXGI.Device3;

[ComImport]
[Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[ComVisible(true)]
interface IDirect3DDxgiInterfaceAccess
{
    IntPtr GetInterface([In] ref Guid iid);
};

public static class Direct3D11Helpers
{
    internal static Guid IInspectable = new Guid("AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90");
    internal static Guid ID3D11Resource = new Guid("dc8e63f3-d12b-4952-b47b-5e45026a862d");
    internal static Guid IDXGIAdapter3 = new Guid("645967A4-1392-4310-A798-8053CE3E93FD");
    internal static Guid ID3D11Device = new Guid("db6f6ddb-ac77-4e88-8253-819df9bbf140");
    internal static Guid ID3D11Texture2D = new Guid("6f15aaf2-d208-4e89-9ab4-489535d34f9c");

    private static Device d3dDevice;
    private static SwapChain swapChain;

    [DllImport(
        "d3d11.dll",
        EntryPoint = "CreateDirect3D11DeviceFromDXGIDevice",
        SetLastError = true,
        CharSet = CharSet.Unicode,
        ExactSpelling = true,
        CallingConvention = CallingConvention.StdCall
    )]
    internal static extern UInt32 CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

    [DllImport(
        "d3d11.dll",
        EntryPoint = "CreateDirect3D11SurfaceFromDXGISurface",
        SetLastError = true,
        CharSet = CharSet.Unicode,
        ExactSpelling = true,
        CallingConvention = CallingConvention.StdCall
    )]
    internal static extern UInt32
        CreateDirect3D11SurfaceFromDXGISurface(IntPtr dxgiSurface, out IntPtr graphicsSurface);

    public static Device InitializeDirectX()
    {
        // 1. Define the Swap Chain parameters
        var desc = new SwapChainDescription()
        {
            BufferCount = 1,
            ModeDescription = new ModeDescription(
                1920,
                1080,
                new Rational(60, 1),
                Format.R8G8B8A8_UNorm),
            IsWindowed = true,
            // OutputHandle = ,
            SampleDescription = new SampleDescription(1, 0),
            SwapEffect = SwapEffect.Discard,
            Usage = Usage.RenderTargetOutput
        };

        // 2. Create the Device and SwapChain
        // We use FeatureLevel.Level_11_0 to support most modern GPUs
        Device.CreateWithSwapChain(
            DriverType.Hardware,
            DeviceCreationFlags.None,
            new[] { FeatureLevel.Level_11_0 },
            desc,
            out d3dDevice,
            out swapChain);

        return d3dDevice;
    }

    public static IDirect3DDevice CreateD3DDevice()
    {
        return CreateD3DDevice(false);
    }

    public static IDirect3DDevice CreateD3DDevice(bool useWARP)
    {
        var d3dDevice = new Device(
            useWARP ? DriverType.Software : DriverType.Hardware,
            DeviceCreationFlags.BgraSupport);
        IDirect3DDevice device = null;

        // var d3dDevice = InitializeDirectX();

        // Acquire the DXGI interface for the Direct3D device.
        using (var dxgiDevice = d3dDevice.QueryInterface<Device3>())
        {
            // Wrap the native device using a WinRT interop object.
            uint hr = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer, out IntPtr pUnknown);

            if (hr == 0)
            {
                device = Marshal.GetObjectForIUnknown(pUnknown) as IDirect3DDevice;
                Marshal.Release(pUnknown);
            }
        }

        return device;
    }


    internal static IDirect3DSurface CreateDirect3DSurfaceFromSharpDXTexture(Texture2D texture)
    {
        IDirect3DSurface surface = null;

        // Acquire the DXGI interface for the Direct3D surface.
        using (var dxgiSurface = texture.QueryInterface<Surface>())
        {
            // Wrap the native device using a WinRT interop object.
            uint hr = CreateDirect3D11SurfaceFromDXGISurface(dxgiSurface.NativePointer, out IntPtr pUnknown);

            if (hr == 0)
            {
                surface = Marshal.GetObjectForIUnknown(pUnknown) as IDirect3DSurface;
                Marshal.Release(pUnknown);
            }
        }

        return surface;
    }


    internal static Device CreateSharpDXDevice(IDirect3DDevice device)
    {
        var access = (IDirect3DDxgiInterfaceAccess)device;
        var d3dPointer = access.GetInterface(ID3D11Device);
        var d3dDevice = new Device(d3dPointer);
        return d3dDevice;
    }

    internal static Texture2D CreateSharpDXTexture2D(IDirect3DSurface surface)
    {
        var access = (IDirect3DDxgiInterfaceAccess)surface;
        var d3dPointer = access.GetInterface(ID3D11Texture2D);
        var d3dSurface = new Texture2D(d3dPointer);
        return d3dSurface;
    }


    public static Texture2D InitializeComposeTexture(
        Device sharpDxD3dDevice,
        SizeInt32 size)
    {
        var description = new Texture2DDescription
        {
            Width = size.Width,
            Height = size.Height,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.B8G8R8A8_UNorm,
            SampleDescription = new SampleDescription()
            {
                Count = 1,
                Quality = 0
            },
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None
        };
        var composeTexture = new Texture2D(sharpDxD3dDevice, description);


        using (var renderTargetView = new RenderTargetView(sharpDxD3dDevice, composeTexture))
        {
            sharpDxD3dDevice.ImmediateContext.ClearRenderTargetView(renderTargetView,
                new RawColor4(0, 0, 0, 1));
        }

        return composeTexture;
    }
}