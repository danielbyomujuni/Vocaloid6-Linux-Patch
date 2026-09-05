using System;
using System.Runtime.CompilerServices;

// Loaded by the .NET host via DOTNET_STARTUP_HOOKS before the app's Main.
// Must be named StartupHook, no namespace, with a static Initialize().
internal class StartupHook
{
    public static void Initialize()
    {
        try
        {
            string exe = System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath ?? string.Empty);
            if (!string.Equals(exe, "VOCALOID6", StringComparison.OrdinalIgnoreCase))
                return;
            Install();
        }
        catch
        {
            // Never break app startup because of the skin.
        }
    }

    // Separate non-inlined method so WPF assemblies are only resolved once the
    // process-name gate has passed (the hook env var applies prefix-wide).
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Install() => Vocaloid6UiPatch.Patch.Install();
}
