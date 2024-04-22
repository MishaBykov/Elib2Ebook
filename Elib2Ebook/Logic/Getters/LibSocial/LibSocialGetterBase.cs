using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.SocialLib;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters.LibSocial; 

public abstract class LibSocialGetterBase : GetterBase {
    public LibSocialGetterBase(BookGetterConfig config) : base(config) { }

    protected override string GetId(Uri url) => url.GetSegment(1);

    public override Task Init() {
        base.Init();
        Config.Client.DefaultRequestHeaders.Add("Referer", SystemUrl.ToString());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Авторизация в системе
    /// </summary>
    /// <exception cref="Exception"></exception>
    public override async Task Authorize() {
        if (!Config.HasCredentials) {
            return;
        }
        
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(SystemUrl);
        var token = doc.QuerySelector("meta[name=_token]")?.Attributes["content"]?.Value;
        using var post = await Config.Client.PostAsync(SystemUrl.MakeRelativeUri("login"), GenerateAuthData(token));
    }

    private MultipartFormDataContent GenerateAuthData(string token) {
        return new() {
            {new StringContent(token), "_token"},
            {new StringContent(Config.Options.Login), "email"},
            {new StringContent(Config.Options.Password), "password"},
            {new StringContent("on"), "remember"}
        };
    }

    public override async Task<Book> Get(Uri url) {
        var bidId = url.GetQueryParameter("bid");
        url = SystemUrl.MakeRelativeUri(GetId(url));
        Console.WriteLine($"URL: {url}");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        var header = doc.QuerySelector("h4.modal__title.text-danger");
        if (header != default && header.GetText() == "Доступ ограничен 18+") {
            throw new Exception("Произведение доступно только зарегистрированным пользователям. Добавьте в параметры вызова свои логин и пароль");
        }

        var data = GetData(doc);

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(data, url, bidId),
            Title = doc.QuerySelector("meta[property=og:title]").Attributes["content"].Value.Trim(),
            Author = GetAuthor(doc, url),
            Annotation = doc.QuerySelector("div.media-description__text")?.InnerHtml
        };
            
        return book;
    }

    private Author GetAuthor(HtmlDocument doc, Uri url) {
        foreach (var div in doc.QuerySelectorAll("div.media-info-list__item")) {
            var title = div.GetTextBySelector("div.media-info-list__title");
            var value = div.QuerySelector("div.media-info-list__value a");
            if (title == "Автор" && value != default) {
                return new Author(value.GetText(), url.MakeRelativeUri(value.Attributes["href"].Value));
            }
        }

        var logo = doc.QuerySelector("a.header__logo img[alt]");
        return logo == default ? new Author(SystemUrl.Host) : new Author(logo.Attributes["alt"].Value);
    }

    public virtual WindowData GetData(HtmlDocument doc) {
        var match = new Regex("window.__DATA__ = (?<data>{.*}).*window._SITE_COLOR_", RegexOptions.Compiled | RegexOptions.Singleline).Match(doc.Text).Groups["data"].Value;
        var windowData = match.Deserialize<WindowData>();
        windowData.Chapters.List.Reverse();
        return windowData;
    }
    
    protected abstract Task<HtmlDocument> GetChapter(Uri url, SocialLibChapter chapter, User user);

    private async Task<IEnumerable<Chapter>> FillChapters(WindowData data, Uri url, string bidId) {
        var result = new List<Chapter>();
        var branchId = string.IsNullOrWhiteSpace(bidId)
            ? data.Chapters.List
                .GroupBy(c => c.BranchId)
                .MaxBy(c => c.Count())!
                .Key
            : int.Parse(bidId);

        foreach (var ranobeChapter in SliceToc(data.Chapters.List.Where(c => c.BranchId == branchId).ToList())) {
            Console.WriteLine($"Загружаю главу {ranobeChapter.GetName()}");
            var chapter = new Chapter();
            
            var chapterDoc = await GetChapter(url, ranobeChapter, data.User);
            chapter.Images = await GetImages(chapterDoc, SystemUrl);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            chapter.Title = ranobeChapter.GetName();

            result.Add(chapter);
        }

        return result;
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("meta[property=og:image]").Attributes["content"].Value.Trim();
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(uri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }
}