using Orchard.ContentManagement;
using Orchard.ContentManagement.FieldStorage;

namespace Contrib.HtmlField.Fields {
    public class HtmlField : ContentField {
        public string Text {
            get { return Storage.Get<string>(); }
            set { Storage.Set(value); }
        }
    }
}