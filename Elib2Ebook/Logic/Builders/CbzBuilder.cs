using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;

namespace Elib2Ebook.Logic.Builders; 

public class CbzBuilder : BuilderBase {
    private IEnumerable<Chapter> _chapters;
    
    public static BuilderBase Create() {
        return new CbzBuilder();
    }

    public override BuilderBase AddAuthor(Author author) {
        return this;
    }

    public override BuilderBase AddCoAuthors(IEnumerable<Author> coAuthors) {
        return this;
    }

    public override BuilderBase WithTitle(string title) {
        return this;
    }

    public override BuilderBase WithCover(Image cover) {
        return this;
    }

    public override BuilderBase WithBookUrl(Uri url) {
        return this;
    }

    public override BuilderBase WithAnnotation(string annotation) {
        return this;
    }

    public override BuilderBase WithFiles(string directory, string searchPattern) {
        return this;
    }

    public override BuilderBase WithChapters(IEnumerable<Chapter> chapters) {
        _chapters = chapters;
        return this;
    }

    public override BuilderBase WithSeria(Seria seria) {
        return this;
    }

    public override BuilderBase WithLang(string lang) {
        return this;
    }

    protected override async Task BuildInternal(string name) {
        if (File.Exists(name)) {
            File.Delete(name);
        }
        
        using var archive = ZipFile.Open(name, ZipArchiveMode.Create);
        
        var c = 0;
        foreach (var chapter in _chapters) {
            if (chapter.Images == default) {
                continue;
            }

            foreach (var image in chapter.Images) {
                var entry = archive.CreateEntry($"{++c}.{image.Extension}", CompressionLevel.Optimal);
                await using var entryStream = entry.Open();
                await using var fileStream = image.GetStream();
                await fileStream.CopyToAsync(entryStream);
            }
        }
    }

    protected override string GetFileName(string name) {
        return $"{name}.cbz".RemoveInvalidChars();
    }
}