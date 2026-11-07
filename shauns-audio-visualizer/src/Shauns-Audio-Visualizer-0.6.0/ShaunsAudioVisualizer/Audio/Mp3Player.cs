using System.Runtime.InteropServices;
using System.Text;

namespace ShaunsAudioVisualizer.Audio;

internal static class Mp3Player
{
    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    private static extern int mciSendString(
        string command,
        StringBuilder? returnValue,
        int returnLength,
        IntPtr hwndCallback);

    public static void PlayEmbedded(string resourceName)
    {
        try
        {
            var assembly = typeof(Mp3Player).Assembly;
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                return;
            }

            var tempPath = Path.Combine(Path.GetTempPath(), "audiovisualizer-chime.mp3");
            using (var file = File.Create(tempPath))
            {
                stream.CopyTo(file);
            }

            mciSendString($"open \"{tempPath}\" type mpegvideo alias avchime", null, 0, IntPtr.Zero);
            mciSendString("play avchime", null, 0, IntPtr.Zero);
        }
        catch
        {
            // Best-effort playback for the hidden chime.
        }
    }
}