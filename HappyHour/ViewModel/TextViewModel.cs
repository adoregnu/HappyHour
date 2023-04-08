using ICSharpCode.AvalonEdit.Document;

namespace HappyHour.ViewModel
{
    class TextViewModel : Pane
    {
        protected int _lastLineCount = 1;
        protected TextDocument _document = new();
        public bool AutoScrollEnabled { get; set; } = true;
        public TextDocument Document
        {
            get { return _document; }
        }
        protected void AppendText(string line)
        {
            _document.Insert(_document.TextLength, line);
            _lastLineCount = _document.LineCount;
        }

        //public virtual void AddElementGenerator(TextView view) { }

        public override void Cleanup()
        {
            base.Cleanup();
            _document.Remove(0, _document.TextLength);
        }
    }
}
