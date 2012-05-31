using Contrib.ImageField.Settings;

namespace Contrib.ImageField.ViewModels {
    public class ImageFieldViewModel {
        public ImageFieldSettings Settings { get; set; }
        public Fields.ImageField Field { get; set; }
        public string AlternateText { get; set; }
        public bool Removed { get; set; }
    }
}