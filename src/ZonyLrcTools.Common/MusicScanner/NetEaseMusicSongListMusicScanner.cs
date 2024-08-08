public class NetEaseMusicSongListMusicScanner : ISingletonDependency
{
    private readonly IWarpHttpClient _warpHttpClient;
    private readonly ILogger<NetEaseMusicSongListMusicScanner> _logger;
    private const string Host = "https://music.163.com";

    private string Cookie { get; set; } = string.Empty;
    private string CsrfToken { get; set; } = string.Empty;

    public NetEaseMusicSongListMusicScanner(IWarpHttpClient warpHttpClient,
        ILogger<NetEaseMusicSongListMusicScanner> logger)
    {
        _warpHttpClient = warpHttpClient;
        _logger = logger;
    }

    public async Task<List<MusicInfo>> GetMusicInfoFromNetEaseMusicSongListAsync(string songListIds, string outputDirectory, string pattern)
    {
        if (string.IsNullOrEmpty(Cookie))
        {
            var loginResponse = await LoginViaQrCodeAsync();
            Cookie = loginResponse.cookieContainer?.GetCookieHeader(new Uri(Host)) ?? string.Empty;
            CsrfToken = loginResponse.csrfToken ?? string.Empty;
        }

        async Task<List<MusicInfo>> GetMusicInfoBySongIdAsync(string songId)
        {
            var secretKey = NetEaseMusicEncryptionHelper.CreateSecretKey(16);
            var encSecKey = NetEaseMusicEncryptionHelper.RsaEncode(secretKey);
            var response = await _warpHttpClient.PostAsync<GetMusicInfoFromNetEaseMusicSongListResponse>(
                $"{Host}/weapi/v6/playlist/detail?csrf_token={CsrfToken}", requestOption:
                request =>
                {
                    request.Headers.Add("Cookie", Cookie);
                    request.Content = new FormUrlEncodedContent(HandleRequest(new
                    {
                        csrf_token = CsrfToken,
                        id = songId,
                        n = 1000,
                        offset = 0,
                        total = true,
                        limit = 1000,
                    }, secretKey, encSecKey));
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                });

            if (response.Code != 200 || response.PlayList?.SongList == null)
            {
                throw new ErrorCodeException(ErrorCodes.NotSupportedFileEncoding);
            }

            return response.PlayList.SongList
                .Where(song => !string.IsNullOrEmpty(song.Name))
                .Select(song =>
                {
                    var artistName = song.Artists?.FirstOrDefault()?.Name ?? string.Empty;
                    var album = song.Album?.Name ?? string.Empty; // 如果有专辑信息
                    var duration = song.Duration; // 如果有时长信息
                    return new MusicInfo(song.Name!, artistName, song.SongId, album, duration);
                }).ToList();
        }

        var musicInfoList = new List<MusicInfo>();
        foreach (var songListId in songListIds.Split(';'))
        {
            _logger.LogInformation("正在获取歌单 {SongListId} 的歌曲列表。", songListId);
            var musicInfos = await GetMusicInfoBySongIdAsync(songListId);
            musicInfoList.AddRange(musicInfos);
        }

        return musicInfoList;
    }

    private Dictionary<string, string> HandleRequest(object srcParams, string secretKey, string encSecKey)
    {
        return new Dictionary<string, string>
        {
            {
                "params", NetEaseMusicEncryptionHelper.AesEncode(
                    NetEaseMusicEncryptionHelper.AesEncode(
                        JsonConvert.SerializeObject(srcParams), NetEaseMusicEncryptionHelper.Nonce), secretKey)
            },
            { "encSecKey", encSecKey }
        };
    }

    private async Task<(string? csrfToken, CookieContainer? cookieContainer)> LoginViaQrCodeAsync()
    {
        var qrCodeKeyJson = await (await PostAsync($"{Host}/weapi/login/qrcode/unikey", new
        {
            type = 1
        })).Content.ReadAsStringAsync();
        var uniKey = JObject.Parse(qrCodeKeyJson).SelectToken("$.unikey")!.Value<string>();
        if (string.IsNullOrEmpty(uniKey)) return (null, null);

        var qrCodeLink = $"{Host}/login?codekey={uniKey}";

        var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(qrCodeLink, QRCodeGenerator.ECCLevel.L);
        var qrCode = new AsciiQRCode(qrCodeData);
        var asciiQrCodeString = qrCode.GetGraphic(1, drawQuietZones: false);

        _logger.LogInformation("请使用网易云 APP 扫码登录:");
        _logger.LogInformation("\n{AsciiQrCodeString}", asciiQrCodeString);
        _logger.LogInformation("登录链接:");
        _logger.LogInformation(qrCodeLink);

        var isLogin = false;
        while (!isLogin)
        {
            var (isSuccess, cookieContainer) = await CheckIsLoginAsync(uniKey);
            isLogin = isSuccess;

            if (!isLogin)
            {
                await Task.Delay(2000);
            }
            else
            {
                return (cookieContainer?.GetCookies(new Uri(Host))["__csrf"]?.Value, cookieContainer);
            }
        }

        return (null, null);
    }

    private async Task<(bool isSuccess, CookieContainer? cookieContainer)> CheckIsLoginAsync(string uniKey)
    {
        var responseMessage = await PostAsync($"{Host}/weapi/login/qrcode/client/login", new
        {
            key = uniKey,
            type = 1
        });

        var responseString = await responseMessage.Content.ReadAsStringAsync();
        var responseCode = JObject.Parse(responseString)["code"]?.Value<int>();

        if (responseCode != 803)
        {
            return (false, null);
        }

        if (!responseMessage.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return (false, null);
        }

        var cookieContainer = new CookieContainer();
        foreach (var cookie in cookies)
        {
            cookieContainer.SetCookies(new Uri(Host), cookie);
        }

        return (true, cookieContainer);
    }

    private async Task<HttpResponseMessage> PostAsync(string url, object @params)
    {
        var secretKey = NetEaseMusicEncryptionHelper.CreateSecretKey(16);
        var encSecKey = NetEaseMusicEncryptionHelper.RsaEncode(secretKey);

        return await _warpHttpClient.PostReturnHttpResponseAsync(url, requestOption:
            request =>
            {
                request.Content = new FormUrlEncodedContent(HandleRequest(@params, secretKey, encSecKey));
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            });
    }
}
