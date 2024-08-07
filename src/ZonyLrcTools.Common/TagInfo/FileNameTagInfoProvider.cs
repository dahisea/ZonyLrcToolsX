using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using ZonyLrcTools.Common;
using ZonyLrcTools.Common.Configuration;
using ZonyLrcTools.Common.Infrastructure.DependencyInject;

namespace ZonyLrcTools.Cli.Infrastructure.Tag
{
    /// <summary>
    /// 基于正则表达式的标签解析器，从文件名当中解析歌曲的标签信息。
    /// </summary>
    public class FileNameTagInfoProvider : ITagInfoProvider, ISingletonDependency
    {
        public string Name => ConstantName;
        public const string ConstantName = "FileName";
        public const string RegularExpressionsOption = "regularExpressions";

        private readonly GlobalOptions _options;

        public FileNameTagInfoProvider(IOptions<GlobalOptions> options)
        {
            _options = options.Value;
        }

        public ValueTask<MusicInfo?> LoadAsync(string filePath)
        {
            try
            {
                var regex = _options.Provider.Tag.Plugin
                    .First(t => t.Name == ConstantName)
                    .Extensions[RegularExpressionsOption];

                var match = Regex.Match(Path.GetFileNameWithoutExtension(filePath), regex);

                if (match.Groups.Count != 3)
                {
                    return new ValueTask<MusicInfo?>(result: null);
                }

                var musicInfo = new MusicInfo(filePath, match.Groups["name"].Value, match.Groups["artist"].Value);
                return new ValueTask<MusicInfo?>(result: musicInfo);
            }
            catch (Exception ex)
            {
                // 记录异常日志
                // _logger.LogError(ex, "Error parsing file name.");
                return new ValueTask<MusicInfo?>(result: null);
            }
        }
    }
}
