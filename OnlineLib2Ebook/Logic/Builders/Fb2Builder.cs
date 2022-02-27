using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EpubSharp.Format;
using HtmlAgilityPack;
using OnlineLib2Ebook.Extensions;
using OnlineLib2Ebook.Types.Book;

namespace OnlineLib2Ebook.Logic.Builders; 

public class Fb2Builder : BuilderBase {
    private readonly XElement _book;
    private readonly XNamespace _ns = "http://www.gribuser.ru/xml/fictionbook/2.0";
    private readonly XNamespace _xlink = "http://www.w3.org/1999/xlink";

    private readonly XElement _description;
    private readonly XElement _titleInfo;
    private readonly XElement _body;
    private readonly List<XElement> _images = new();

    private readonly Dictionary<string, string> _map = new() {
        {"strong", "strong"},
        {"b", "strong"},
        {"i", "emphasis"},
        {"em", "emphasis"},
        {"blockquote", "cite"},
    };

    private Fb2Builder() {
        _book = CreateXElement("FictionBook");
        _book.SetAttributeValue(XNamespace.Xmlns + "xlink", _xlink.NamespaceName);
        _description = CreateXElement("description");
        _titleInfo = CreateXElement("title-info");
        _body = CreateXElement("body");
    }

    private XElement CreateXElement(string name) {
        return new XElement(_ns + name);
    }

    private XElement CreateAuthor(string author) {
        var authorElem = CreateXElement("author");
        var nicknameElem = CreateXElement("nickname");
        nicknameElem.Value = author;
            
        authorElem.Add(nicknameElem);
        return authorElem;
    }

    private XElement CreateTitle(string text) {
        var p = CreateXElement("p");
        p.Value = text.HtmlDecode();
            
        var title = CreateXElement("title");
            
        title.Add(p);
        return title;
    }

    private XElement GetBinary(Image image) {
        var binaryElem = new XElement(_ns + "binary");
        binaryElem.Value = Convert.ToBase64String(image.Content);
        binaryElem.SetAttributeValue("id", image.Path);
        binaryElem.SetAttributeValue("content-type", "image/" + image.Extension);

        return binaryElem;
    }

    private static HtmlDocument CreateDoc(string content) {
        var doc = new HtmlDocument();
        doc.LoadHtml(content);
        return doc;
    }

    /// <summary>
    /// Создание нового объекта Builder'a
    /// </summary>
    /// <returns></returns>
    public static BuilderBase Create() {
        return new Fb2Builder();
    }

    /// <summary>
    /// Добавление автора книги
    /// </summary>
    /// <param name="author">Автор</param>
    /// <returns></returns>
    public override BuilderBase AddAuthor(string author) {
        _titleInfo.Add(CreateAuthor(author));
        return this;
    }

    /// <summary>
    /// Указание названия книги
    /// </summary>
    /// <param name="title">Название книги</param>
    /// <returns></returns>
    public override BuilderBase WithTitle(string title) {
        var bookTitle = CreateXElement("book-title");
        bookTitle.Value = title;

        _titleInfo.Add(bookTitle);
        _body.Add(CreateTitle(title));
        return this;
    }

    /// <summary>
    /// Добавление обложки книги
    /// </summary>
    /// <param name="cover">Обложка</param>
    /// <returns></returns>
    public override BuilderBase WithCover(Image cover) {
        if (cover != default) {
            var imageElem = CreateXElement("image");
            imageElem.SetAttributeValue(_xlink + "href", "#" + cover.Path);

            _titleInfo.Add(imageElem);
            _images.Add(GetBinary(cover));
        }

        return this;
    }

    /// <summary>
    /// Добавление внешних файлов
    /// </summary>
    /// <param name="directory">Путь к директории с файлами</param>
    /// <param name="searchPattern">Шаблон поиска файлов</param>
    /// <param name="type">Тип файла</param>
    /// <returns></returns>
    public override BuilderBase WithFiles(string directory, string searchPattern, EpubContentType type) {
        return this;
    }

    /// <summary>
    /// Добавление списка частей книги
    /// </summary>
    /// <param name="chapters">Список частей</param>
    /// <returns></returns>
    public override BuilderBase WithChapters(IEnumerable<Chapter> chapters) {
        foreach (var chapter in chapters.Where(c => c.IsValid)) {
            var section = CreateXElement("section");
            section.Add(CreateTitle(chapter.Title));
                
            var doc = CreateDoc(chapter.Content);
            foreach (var node in doc.DocumentNode.ChildNodes) {
                if (node.Name is "p" or "div") {
                    foreach (var child in node.ChildNodes) {
                        ProcessSection(section, child);
                    }
                } else {
                    ProcessSection(section, node);
                }
            }
                
            _body.Add(section);

            foreach (var image in chapter.Images) {
                _images.Add(GetBinary(image));
            }
        }

        return this;
    }

    private void ProcessSection(XElement section, HtmlNode node) {
        var p = CreateXElement("p");
            
        switch (node.Name) {
            case "#text":
            case "br":
            case "span":
                if (!string.IsNullOrWhiteSpace(node.InnerText)) {
                    p.Add(new XText(node.InnerText));
                }

                break;
            case "img":
                if (node.Attributes["src"] == null) {
                    return;
                }
                    
                var imageElem = CreateXElement("image");
                imageElem.SetAttributeValue(_xlink + "href", "#" + node.Attributes["src"].Value);
                p.Add(imageElem);
                break;
            default:
                if (_map.TryGetValue(node.Name, out var fb2Tag)) {
                    var tag = CreateXElement(fb2Tag);
                    tag.Value = node.InnerText;
                    p.Add(tag);
                } else {
                    p.Add(new XText(node.InnerText));
                    Console.WriteLine(node.Name);
                }

                break;
        }

        if (!p.IsEmpty) {
            section.Add(p);
        }
    }

    protected override void BuildInternal(string name) {
        _description.Add(_titleInfo);
        _book.Add(_description);
        _book.Add(_body);

        foreach (var image in _images) {
            _book.Add(image);
        }
            
        _book.Save(name);
    }

    protected override string GetFileName(string name) {
        return $"{name}.fb2".RemoveInvalidChars();
    }
}