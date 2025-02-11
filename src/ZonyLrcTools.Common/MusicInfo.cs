
using System.Text.RegularExpressions;
using ZonyLrcTools.Common.MusicScanner;

namespace ZonyLrcTools.Common
{
    /// <summary>
    /// 歌曲信息的承载类，携带歌曲的相关数据。
    /// </summary>
    public partial class MusicInfo
    {
        /// <summary>
        /// 歌曲对应的物理文件路径。
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// 歌曲的实际歌曲长度。
        /// </summary>
        public long? TotalTime { get; set; }

        /// <summary>
        /// 歌曲的名称。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 歌曲的作者。
        /// </summary>
        public string Artist { get; set; }
        
        /// <summary>
        /// 歌曲的 Sid。
        /// </summary>
        public string SongId { get; set; }

        /// <summary>
        /// 是否下载成功?
        /// </summary>
        public bool IsSuccessful { get; set; } = true;

        /// <summary>
        /// 是否是纯音乐?
        /// </summary>
        public bool IsPureMusic { get; set; } = false;

        /// <summary>
        /// 构建一个新的 <see cref="MusicInfo"/> 对象。
        /// </summary>
        /// <param name="filePath">歌曲对应的物理文件路径。</param>
        /// <param name="name">歌曲的名称。</param>
        /// <param name="artist">歌曲的作者。</param>
        /// <param name="songId">歌曲的 Sid。</param>
        public MusicInfo(string filePath, string name, string artist, string songId)
        {
            FilePath = Path.Combine(Path.GetDirectoryName(filePath) ?? string.Empty, HandleInvalidFilePath(Path.GetFileName(filePath)));
            Name = name;
            Artist = artist;
            SongId = songId;  // Initialize SongId from the provided argument
        }

        /// <summary>
        /// 处理无效的文件路径字符。
        /// </summary>
        /// <param name="srcText">源文本。</param>
        /// <returns>处理后的文本。</returns>
        private string HandleInvalidFilePath(string srcText)
        {
            return InvalidFilePathRegex().Replace(srcText, "");
        }

        [GeneratedRegex(@"[<>:""/\\|?*]")]
        private static partial Regex InvalidFilePathRegex();

        public static bool operator ==(MusicInfo? left, MusicInfo? right)
        {
            return left?.FilePath == right?.FilePath;
        }

        public static bool operator !=(MusicInfo? left, MusicInfo? right)
        {
            return !(left == right);
        }

        public override bool Equals(object? obj)
        {
            if (obj is MusicInfo other)
            {
                return FilePath == other.FilePath;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return FilePath.GetHashCode();
        }
    }
}