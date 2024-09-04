using System;
using System.IO;
using System.Linq;
using System.Xml;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Core.Extensions; 

public static class HtmlDocumentExtensions {
    public static string AsString(this HtmlDocument self) {
        using var sw = new StringWriter();
        using var xw = new XmlTextWriter(sw);
        self.Save(xw);

        return sw.ToString();
    }

    public static string GetTextBySelector(this HtmlDocument doc, string selector) {
        return doc.DocumentNode.GetTextBySelector(selector);
    }
        
    public static string GetTextBySelector(this HtmlNode node, string selector) {
        return node.QuerySelector(selector).GetText();
    }
        
    public static string GetText(this HtmlNode node) {
        return node?.InnerText?.HtmlDecode();
    }

    public static HtmlDocument RemoveNodes(this HtmlDocument doc, Func<HtmlNode, bool> predicate) {
        doc.DocumentNode.RemoveNodes(predicate);
        return doc;
    }
        
    public static HtmlNode RemoveNodes(this HtmlNode self, Func<HtmlNode, bool> predicate) {
        var toRemove = self.ChildNodes.Where(predicate).ToList();
        foreach (var node in toRemove) {
            node.Remove();
        }

        return self;
    }
    
    public static HtmlNode RemoveNodes(this HtmlNode self, string selector) {
        var toRemove = self.QuerySelectorAll(selector);
        foreach (var node in toRemove) {
            node.Remove();
        }

        return self;
    }
    
    public static HtmlDocument RemoveNodes(this HtmlDocument self, string selector) {
        self.DocumentNode.RemoveNodes(selector);
        return self;
    }
}