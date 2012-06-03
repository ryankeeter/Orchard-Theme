using System.Collections.Generic;
using Orchard.ContentManagement;
using Orchard.ContentManagement.MetaData;
using Orchard.ContentManagement.MetaData.Builders;
using Orchard.ContentManagement.MetaData.Models;
using Orchard.ContentManagement.ViewModels;

namespace Contrib.HtmlField.Settings {
    public class HtmlFieldEditorEvents : ContentDefinitionEditorEventsBase {
        public override IEnumerable<TemplateViewModel> PartFieldEditor(ContentPartFieldDefinition definition) {
            if (definition.FieldDefinition.Name == "HtmlField") {
                var model = definition.Settings.GetModel<HtmlFieldSettings>();
                yield return DefinitionTemplate(model);
            }
        }

        public override IEnumerable<TemplateViewModel> PartFieldEditorUpdate(ContentPartFieldDefinitionBuilder builder, IUpdateModel updateModel) {
            var model = new HtmlFieldSettings();
            if (updateModel.TryUpdateModel(model, "HtmlFieldSettings", null, null)) {
                builder.WithSetting("HtmlFieldSettings.FlavorDefault", model.FlavorDefault);
            }

            yield return DefinitionTemplate(model);
        }
    }
}