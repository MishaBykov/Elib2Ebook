namespace Author.Today.Epub.Converter.Types.Litnet.Response {
    public class LitnetChapter {
        public string Id;
        public string Name;

        public LitnetChapter(string id, string name) {
            Id = id;
            Name = name;
        }
    }
}