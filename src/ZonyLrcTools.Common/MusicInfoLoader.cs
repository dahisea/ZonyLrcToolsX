using Microsoft.Extensions.Options;
using ZonyLrcTools.Common.Configuration;
using ZonyLrcTools.Common.Infrastructure.DependencyInject;
using ZonyLrcTools.Common.Infrastructure.Exceptions;
using ZonyLrcTools.Common.Infrastructure.IO;
using ZonyLrcTools.Common.Infrastructure.Logging;
using ZonyLrcTools.Common.Infrastructure.Threading;

namespace ZonyLrcTools.Common;

public class MusicInfoLoader : IMusicInfoLoader, ITransientDependency
{
    private readonly IWarpLogger _logger;
    private readonly GlobalOptions _options;

    public MusicInfoLoader(IWarpLogger logger,
        IOptions<GlobalOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }


    private List<string> RemoveExistLyricFiles(IEnumerable<string> filePaths)
    {
        if (!_options.Provider.Lyric.Config.IsSkipExistLyricFiles)
        {
            return filePaths.ToList();
        }

        return filePaths
            .Where(path =>
            {
                if (!File.Exists(Path.ChangeExtension(path, ".lrc")))
                {
                    return true;
                }

                _logger.WarnAsync($"已经存在歌词文件 {path}，跳过。").GetAwaiter().GetResult();
                return false;
            })
            .ToList();
    }
}