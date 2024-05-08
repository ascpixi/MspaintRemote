using System.Diagnostics;
using System.Runtime.InteropServices;
using MspaintRemote.Internal;
using MspaintRemote.Native;
using MspaintRemote.Native.Types;

namespace MspaintRemote;

/// <summary>
/// Allows remotely controlling Microsoft Paint instances.
/// </summary>
public class MspaintController : IDisposable
{
    nint process;
    nuint baseAddr;
    List<nuint> canvasAddresses = [];
    nint hwnd;
    int lastWidth, lastHeight;
    MspaintAddressSet addresses;

    /// <summary>
    /// Constructs a new <see cref="MspaintController"/> class by using an existing <c>mspaint.exe</c>
    /// process or by starting a new one.
    /// </summary>
    /// <param name="startNew">If <see langword="true"/>, a new Paint process will be created.</param>
    public MspaintController(bool startNew = false) : this(GetPaintProcess(startNew)) { }
    
    /// <summary>
    /// Constructs a new <see cref="MspaintController"/> class that will interface with
    /// the given Paint instance.
    /// </summary>
    /// <param name="paint">The target Paint process.</param>
    /// <exception cref="Exception">Thrown when the controller can't open a memory access handle to the process.</exception>
    public MspaintController(Process paint)
    {
        baseAddr = (nuint)paint.MainModule!.BaseAddress;
        hwnd = paint.MainWindowHandle;
        
        process = Kernel32.OpenProcess(
            ProcessAccess.VmRead | ProcessAccess.VmWrite | ProcessAccess.VmOperation,
            false,
            (uint)paint.Id
        );

        if (process == 0)
            throw new Exception($"Couldn't open a memory access handle to Paint: {Marshal.GetLastPInvokeErrorMessage()}");
        
        addresses.Initialize(this);
    }
    
    static Process GetPaintProcess(bool startNew)
    {
        if (startNew) {
            var started = Process.Start("mspaint.exe");
            Thread.Sleep(1000);
            return started;
        }
        
        var paint = Process.GetProcessesByName("mspaint").FirstOrDefault()!;
        if (paint == null)
            throw new Exception("No instance of mspaint.exe is currently running.");

        return paint;
    }
    
    /// <summary>
    /// The width of the canvas.
    /// </summary>
    public int CanvasWidth => ReadArbitrary<int>(baseAddr + MspaintOffsets.CanvasWidth);

    /// <summary>
    /// The height of the canvas.
    /// </summary>
    public int CanvasHeight => ReadArbitrary<int>(baseAddr + MspaintOffsets.CanvasHeight);

    /// <summary>
    /// The value of "Color 1" - the primary color.
    /// </summary>
    /// <remarks>Changing this value will not update the UI.</remarks>
    public Color24 PrimaryColor {
        get => ReadArbitrary<Color24>(baseAddr + MspaintOffsets.PrimaryBrush);
        set  => WriteArbitrary(baseAddr + MspaintOffsets.PrimaryBrush, value);
    }

    /// <summary>
    /// The value of "Color 2" - the secondary color.
    /// </summary>
    /// <remarks>Changing this value will not update the UI.</remarks>
    public Color24 SecondaryColor {
        get => ReadArbitrary<Color24>(baseAddr + MspaintOffsets.SecondaryBrush);
        set => WriteArbitrary(baseAddr + MspaintOffsets.SecondaryBrush, value);
    }

    /// <summary>
    /// The tool the user is currently using.
    /// </summary>
    /// <remarks>The setter can often introduce buggy behavior. Avoid it if possible.</remarks>
    public MspaintTool Tool {
        get => ReadArbitrary<MspaintTool>(baseAddr + MspaintOffsets.Tool);
        set {
            WriteArbitrary(baseAddr + MspaintOffsets.Tool, value);
            ForceUpdate();
        }
    }

    /// <summary>
    /// The current font size, in points. Only available after the user has iteracted
    /// with the text tool - otherwise, the value is undefined, and as such, returns <see langword="null"/>.
    /// </summary>
    public int? FontSize => ReadArbitrary<int>(addresses.FontSize);

    /// <summary>
    /// The zoom level, in Paint's native float units. The convertion table is as follows:
    /// <list type="bullet">
    ///     <item>12.50%: 1.5</item>
    ///     <item>25%: 1.625</item>
    ///     <item>50%: 1.75</item>
    ///     <item>100%: 1.875</item>
    ///     <item>200%: 2</item>
    ///     <item>300%: 2.125</item>
    ///     <item>600%: 2.375</item>
    ///     <item>700%: 2.4375</item>
    ///     <item>800%: 2.5</item>
    /// </list>
    /// </summary>
    public float RawZoom => ReadArbitrary<float>(addresses.Zoom);

    /// <summary>
    /// The zoom level, as displayed in Paint's UI. A value of 1.0 means 100%.
    /// </summary>
    public float Zoom {
        get {
            return RawZoom switch {
                1.5f    => 12.50f / 100f,
                1.625f  => 25f / 100f,
                1.75f   => 50f / 100f,
                1.875f  => 1,
                2f      => 2,
                2.125f  => 3,
                2.375f  => 6,
                2.4375f => 7,
                2.5f    => 8,
                _       => (RawZoom - 2) / 8 + 2 // should never happen
            };
        }
    }

    void KeyDown(VirtualKey vk) => User32.PostMessage(hwnd, 0x0100, (nuint)vk, (nint)(0x0001 | User32.MapVirtualKey(vk, 0) << 16));
    void KeyUp(VirtualKey vk) => User32.PostMessage(hwnd, 0x0101, (nuint)vk, (nint)(0x0001 | User32.MapVirtualKey(vk, 0) << 16) | 0xC0 << 24);
    
    void SendKeyCommand(VirtualKey key)
    {
        KeyDown(key);
        KeyUp(key);
    }

    /// <summary>
    /// Opens the standard Paint save dialog.
    /// </summary>
    public void OpenSaveDialog() => SendKeyCommand(VirtualKey.F12);
    
    unsafe void EnsureCanvasFound()
    {
        int nowWidth = CanvasWidth, nowHeight = CanvasHeight;
        
        if (canvasAddresses.Count != 0 && lastWidth == nowWidth && lastHeight == nowHeight)
            return;

        canvasAddresses.Clear();

        float pages = (nowWidth * nowHeight * 3) / 4096f;
        var targetSize = (nuint)(Math.Ceiling(pages) * 4096);

        nuint addr = 0;
        while (Kernel32.VirtualQueryEx(process, addr, out var mbi, sizeof(MemoryBasicInformation)) != 0) {
            addr = mbi.BaseAddress + mbi.RegionSize;

            if (mbi is { Protect: 0x04, State: 0x1000, Type: 0x20000 } && mbi.RegionSize == targetSize) {
                canvasAddresses.Add(mbi.BaseAddress);
                lastWidth = nowWidth;
                lastHeight = nowHeight;
            }
        }

        if (canvasAddresses.Count == 0)
            throw new Exception("Couldn't find any possible memory region that could hold the canvas.");
    }

    unsafe void ForceUpdate()
        => User32.RedrawWindow(
            hwnd,
            null, 0,
            RedrawFlags.Invalidate | RedrawFlags.InternalPaint | RedrawFlags.AllChildren
            | RedrawFlags.EraseNow | RedrawFlags.UpdateNow
        );
    
    /// <summary>
    /// Reads the Paint canvas to the given buffer of <see cref="Color24"/> structures.
    /// </summary>
    /// <remarks>If the buffer is smaller, only a portion of the region will be read, so that it completely fills the buffer.</remarks>
    public void ReadCanvas(Span<Color24> buffer, int offset = 0) => ReadCanvas<Color24>(buffer, offset);
    
    /// <summary>
    /// Reads the Paint canvas to the given buffer of 3-byte structures. If the given
    /// type is not of three bytes, an exception will be thrown.
    /// </summary>
    /// <remarks>If the buffer is smaller, only a portion of the region will be read, so that it completely fills the buffer.</remarks>
    public unsafe void ReadCanvas<T>(Span<T> buffer, int offset = 0) where T : unmanaged
    {
        if (sizeof(T) != 3)
            throw new Exception($"The type {typeof(T).Name} was expected to be 3 bytes in size (it is {sizeof(T)} bytes long).");
        
        EnsureCanvasFound();

        foreach (nuint canvasAddr in canvasAddresses) {
            ReadArbitrary(
                canvasAddr + ((uint)offset * 3),
                buffer[..(offset + (CanvasWidth * CanvasHeight))]
            );
        }
    }

    /// <summary>
    /// Writes to the Paint canvas from the given buffer of <see cref="Color24"/> structures, and requests
    /// a window redraw after the operation has finished.
    /// </summary>
    /// <remarks>If the buffer is smaller, only a portion of the entire canvas will be updated.</remarks>
    public void UpdateCanvas(Span<Color24> buffer, int offset = 0) => UpdateCanvas<Color24>(buffer, offset);

    /// <summary>
    /// Writes to the Paint canvas from the given buffer of 3-byte structures, and requests
    /// a window redraw after the operation has finished. If the given type is not of three
    /// bytes, an exception will be thrown.
    /// </summary>
    /// <remarks>If the buffer is smaller, only a portion of the entire canvas will be updated.</remarks>
    public unsafe void UpdateCanvas<T>(Span<T> buffer, int offset = 0) where T : unmanaged
    {
        if (sizeof(T) != 3)
            throw new Exception($"The type {typeof(T).Name} was expected to be 3 bytes in size (it is {sizeof(T)} bytes long).");

        EnsureCanvasFound();

        foreach (nuint canvasAddr in canvasAddresses) {
            WriteArbitrary<T>(
                canvasAddr + ((uint)offset * 3),
                buffer[..((CanvasWidth * CanvasHeight) - offset)]
            );
        }
        
        ForceUpdate();
    }
    
    /// <summary>
    /// Reads an arbitrary region of virtual memory from the controlled process, equal in size to T.
    /// </summary>
    public unsafe T ReadArbitrary<T>(nuint addr) where T : unmanaged
    {
        T buffer = default;
        if (!Kernel32.ReadProcessMemory(process, addr, &buffer, sizeof(T)))
            throw new Exception($"Failed to read {sizeof(T)} bytes (type {typeof(T).Name}) from 0x{addr:X}: {Marshal.GetLastPInvokeErrorMessage()}");

        return buffer;
    }

    /// <summary>
    /// Reads an arbitrary region of virtual memory from the controlled process.
    /// </summary>
    public unsafe void ReadArbitrary<T>(nuint addr, Span<T> buffer) where T : unmanaged
    {
        fixed (T* ptr = buffer) {
            if (!Kernel32.ReadProcessMemory(process, addr, ptr, sizeof(T) * buffer.Length))
                throw new Exception($"Failed to read {sizeof(T)} bytes (span of type {typeof(T).Name}) from 0x{addr:X}: {Marshal.GetLastPInvokeErrorMessage()}");
        }
    }

    /// <summary>
    /// Writes the given structure to an arbitrary region of virtual memory of the controlled process. 
    /// </summary>
    public unsafe void WriteArbitrary<T>(nuint addr, T value) where T : unmanaged
    {
        if (!Kernel32.WriteProcessMemory(process, addr, &value, sizeof(T)))
            throw new Exception($"Failed to write {sizeof(T)} bytes (type {typeof(T).Name}) to 0x{addr:X}: {Marshal.GetLastPInvokeErrorMessage()}");
    }

    /// <summary>
    /// Writes data to an arbitrary region of virtual memory of the controlled process. 
    /// </summary>
    public unsafe void WriteArbitrary<T>(nuint addr, ReadOnlySpan<T> buffer) where T : unmanaged
    {
        fixed (T* ptr = buffer) {
            if (!Kernel32.WriteProcessMemory(process, addr, ptr, sizeof(T) * buffer.Length))
                throw new Exception($"Failed to write {sizeof(T)} bytes (span of type {typeof(T).Name}) to 0x{addr:X}: {Marshal.GetLastPInvokeErrorMessage()}");
        }
    }

    T? ReadArbitrary<T>(DependentAddress addr) where T : unmanaged
    {
        var result = addr.Value;
        if (result == null)
            return null;

        return ReadArbitrary<T>(result.Value);
    }

    void WriteArbitrary<T>(DependentAddress addr, T value) where T : unmanaged
    {
        var result = addr.Value;
        if (result == null)
            throw new InvalidOperationException("Cannot write to an uninitialized field.");
        
        WriteArbitrary(result.Value, value);
    }

    internal nuint BaseAddress => baseAddr;
    
    public void Dispose()
    {
        Kernel32.CloseHandle(process);
        GC.SuppressFinalize(this);
    }
}