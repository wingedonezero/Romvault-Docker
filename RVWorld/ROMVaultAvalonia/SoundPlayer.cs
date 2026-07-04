using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ROMVault
{
    internal static class RVPlayer
    {
        public static void PlaySound(string filename)
        {
            try
            {
                filename = filename.Replace('\\', System.IO.Path.DirectorySeparatorChar);
                if (!RVIO.File.Exists(filename))
                    return;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    StartPlayer("powershell", $"-NoProfile -c (New-Object Media.SoundPlayer '{filename}').PlaySync()");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    StartPlayer("afplay", $"\"{filename}\"");
                }
                else
                {
                    // Linux: try PulseAudio/PipeWire first, then ALSA
                    if (!StartPlayer("paplay", $"\"{filename}\""))
                        StartPlayer("aplay", $"-q \"{filename}\"");
                }
            }
            catch { }
        }

        private static bool StartPlayer(string exe, string args)
        {
            try
            {
                Process.Start(new ProcessStartInfo(exe, args)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
