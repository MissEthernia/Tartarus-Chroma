using System.Runtime.InteropServices;

namespace TartarusChroma;

internal sealed class HotkeyWindow : NativeWindow, IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModControl = 0x0002;
    private const uint ModAlt = 0x0001;
    private const uint ModShift = 0x0004;

    public event Action<int>? HotkeyPressed;

    public HotkeyWindow()
    {
        CreateHandle(new CreateParams());
    }

    public void RegisterAll()
    {
        UnregisterAll();

        // Tasten 1–10: Strg + Alt + 1 bis 0
        int[] digitKeys =
        [
            (int)Keys.D1, (int)Keys.D2, (int)Keys.D3, (int)Keys.D4, (int)Keys.D5,
            (int)Keys.D6, (int)Keys.D7, (int)Keys.D8, (int)Keys.D9, (int)Keys.D0
        ];

        for (int index = 0; index < 10; index++)
        {
            if (!RegisterHotKey(Handle, index + 1, ModControl | ModAlt, (uint)digitKeys[index]))
                throw new InvalidOperationException(
                    $"Tastenkürzel Strg+Alt+{(index + 1) % 10} ist bereits belegt.");
        }

        // Tasten 11–20: Strg + Alt + Umschalt + 1 bis 0
        for (int index = 0; index < 10; index++)
        {
            if (!RegisterHotKey(
                    Handle,
                    index + 11,
                    ModControl | ModAlt | ModShift,
                    (uint)digitKeys[index]))
            {
                throw new InvalidOperationException(
                    $"Tastenkürzel Strg+Alt+Umschalt+{(index + 1) % 10} ist bereits belegt.");
            }
        }
    }

    public void UnregisterAll()
    {
        for (int id = 1; id <= 20; id++)
            UnregisterHotKey(Handle, id);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey)
        {
            int id = m.WParam.ToInt32();
            if (id is >= 1 and <= 20)
                HotkeyPressed?.Invoke(id - 1);
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        UnregisterAll();
        DestroyHandle();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(
        IntPtr hWnd,
        int id,
        uint fsModifiers,
        uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
