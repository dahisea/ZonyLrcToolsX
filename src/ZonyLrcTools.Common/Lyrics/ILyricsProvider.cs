using System.Threading.Tasks;

namespace ZonyLrcTools.Common.Lyrics
{
    public interface ILyricsProvider
    {
        string DownloaderName { get; }
        ValueTask<LyricsItemCollection> DownloadAsync(string songName, string artist, string songId, string? duration = null);
    }
}
