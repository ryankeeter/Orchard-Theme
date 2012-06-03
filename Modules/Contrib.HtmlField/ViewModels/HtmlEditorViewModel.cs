namespace Contrib.HtmlField.ViewModels {
    public class HtmlEditorViewModel {
        public Fields.HtmlField HtmlField { get; set; }

        public string Text {
            get { return HtmlField.Text; }
            set { HtmlField.Text = value; }
        }

        public string EditorFlavor { get; set; }
        public string AddMediaPath { get; set; }
    }
}