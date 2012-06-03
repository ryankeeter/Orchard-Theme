using System.Collections.Generic;
using System.Linq;
using System.Web;
using Contrib.HtmlField.Settings;
using Contrib.HtmlField.ViewModels;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Aspects;
using Orchard.ContentManagement.Drivers;
using Orchard.Core.Routable.Models;
using Orchard.Services;

namespace Contrib.HtmlField.Drivers {
    public class HtmlFieldDriver : ContentFieldDriver<Fields.HtmlField> {
        private readonly IEnumerable<IHtmlFilter> _htmlFilters;

        public HtmlFieldDriver(IEnumerable<IHtmlFilter> htmlFilters) {
            _htmlFilters = htmlFilters;
        }

        private static string GetPrefix(ContentField field, ContentPart part) {
            return part.PartDefinition.Name + "." + field.Name;
        }

        private static string GetDifferentiator(ContentField field, ContentPart part) {
            return field.Name;
        }

        protected override DriverResult Display(ContentPart part, Fields.HtmlField field, string displayType, dynamic shapeHelper) {
            var flavor = field.PartFieldDefinition.Settings.GetModel<HtmlFieldSettings>().FlavorDefault;
            return Combined(
                ContentShape("Fields_Contrib_Html", GetDifferentiator(field, part), () => {
                    var htmlText = _htmlFilters.Aggregate(field.Text, (text, filter) => filter.ProcessContent(text, flavor));
                                                                                        return shapeHelper.Fields_Contrib_Html(ContentPart: part, ContentField: field, Html: new HtmlString(htmlText));
                                                                                    }),
                ContentShape("Fields_Contrib_Html_Summary", GetDifferentiator(field, part), () => {
                    var htmlText = _htmlFilters.Aggregate(field.Text, (text, filter) => filter.ProcessContent(text, flavor));
                                                                                                return shapeHelper.Fields_Contrib_Html_Summary(ContentPart: part, ContentField: field, Html: new HtmlString(htmlText));
                                                                                            })
                );
        }

        protected override DriverResult Editor(ContentPart part, Fields.HtmlField field, dynamic shapeHelper) {
            var model = BuildEditorViewModel(part, field);
            return ContentShape("Fields_Contrib_Html_Edit", GetDifferentiator(field, part),
                                () => shapeHelper.EditorTemplate(TemplateName: "Fields/Contrib.Html", Model: model, Prefix: GetPrefix(field, part)));
        }

        protected override DriverResult Editor(ContentPart part, Fields.HtmlField field, IUpdateModel updater, dynamic shapeHelper) {
            var model = BuildEditorViewModel(part, field);
            updater.TryUpdateModel(model, GetPrefix(field, part), null, null);
            return Editor(part, field, shapeHelper);
        }

        private static HtmlEditorViewModel BuildEditorViewModel(ContentPart part, Fields.HtmlField field) {
            return new HtmlEditorViewModel {
                HtmlField = field,
                Text = field.Text,
                EditorFlavor = field.PartFieldDefinition.Settings.GetModel<HtmlFieldSettings>().FlavorDefault,
                AddMediaPath = new PathBuilder(part).AddContentType().AddContainerSlug().AddSlug().ToString()
            };
        }
        
        private class PathBuilder {
            private readonly IContent _content;
            private string _path;

            public PathBuilder(IContent content) {
                _content = content;
                _path = "";
            }

            public override string ToString() {
                return _path;
            }

            public PathBuilder AddContentType() {
                Add(_content.ContentItem.ContentType);
                return this;
            }

            public PathBuilder AddContainerSlug() {
                var common = _content.As<ICommonPart>();
                if (common == null) {
                    return this;
                }

                var routable = common.Container.As<RoutePart>();
                if (routable == null) {
                    return this;
                }

                Add(routable.Slug);
                return this;
            }

            public PathBuilder AddSlug() {
                var routable = _content.As<RoutePart>();
                if (routable == null) {
                    return this;
                }

                Add(routable.Slug);
                return this;
            }

            private void Add(string segment) {
                if (string.IsNullOrEmpty(segment)) {
                    return;
                }
                if (string.IsNullOrEmpty(_path)) {
                    _path = segment;
                }
                else {
                    _path = _path + "/" + segment;
                }
            }
        }
    }
}