﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Contrib.Taxonomies.Models;
using Orchard;
using Orchard.Autoroute.Models;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Aspects;
using Orchard.ContentManagement.MetaData;
using Orchard.Core.Common.Models;
using Orchard.Core.Title.Models;
using Orchard.Data;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Security;
using Orchard.UI.Notify;

namespace Contrib.Taxonomies.Services {

    public class TaxonomyService : ITaxonomyService {
        private readonly IRepository<TermContentItem> _termContentItemRepository;
        private readonly IContentManager _contentManager;
        private readonly INotifier _notifier;
        private readonly IAuthorizationService _authorizationService;
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly IOrchardServices _services;

        public TaxonomyService(
            IRepository<TermContentItem> termContentItemRepository,
            IContentManager contentManager,
            INotifier notifier,
            IContentDefinitionManager contentDefinitionManager,
            IAuthorizationService authorizationService,
            IOrchardServices services) {
            _termContentItemRepository = termContentItemRepository;
            _contentManager = contentManager;
            _notifier = notifier;
            _authorizationService = authorizationService;
            _contentDefinitionManager = contentDefinitionManager;
            _services = services;

            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }

        public IEnumerable<TaxonomyPart> GetTaxonomies() {
            return _contentManager.Query<TaxonomyPart>().List();
        }

        public TaxonomyPart GetTaxonomy(int id) {
            return _contentManager.Get(id).As<TaxonomyPart>();
        }

        public TaxonomyPart GetTaxonomyByName(string name) {
            if (String.IsNullOrWhiteSpace(name)) {
                throw new ArgumentNullException("name");
            }

            return _contentManager
                .Query<TaxonomyPart>()
                .Join<TitlePartRecord>()
                .Where(r => r.Title == name)
                .List()
                .FirstOrDefault();
        }

        public TaxonomyPart GetTaxonomyBySlug(string slug) {
            if (String.IsNullOrWhiteSpace(slug)) {
                throw new ArgumentNullException("slug");
            }

            return _contentManager
                .Query<TaxonomyPart, TaxonomyPartRecord>()
                .Join<AutoroutePartRecord>()
                .Where(r => r.DisplayAlias == slug)
                .List()
                .FirstOrDefault();
        }

        public void CreateTermContentType(TaxonomyPart taxonomy) {
            // create the associated term's content type
            taxonomy.TermTypeName = GenerateTermTypeName(taxonomy.Name);

            _contentDefinitionManager.AlterTypeDefinition(taxonomy.TermTypeName, 
                cfg => cfg
                    .WithSetting("Taxonomy", taxonomy.Name)
                    .WithPart("TermPart")
                    .WithPart("TitlePart")
                    .WithPart("AutoroutePart", builder => builder
                        .WithSetting("AutorouteSettings.AllowCustomPattern", "true")
                        .WithSetting("AutorouteSettings.AutomaticAdjustmentOnEdit", "false")
                        .WithSetting("AutorouteSettings.PatternDefinitions", "[{Name:'Taxonomy and Title', Pattern: '{Content.Container.Path}/{Content.Slug}', Description: 'my-taxonomy/my-term/sub-term'}]")
                        .WithSetting("AutorouteSettings.DefaultPatternIndex", "0"))
                    .WithPart("CommonPart")
                    .DisplayedAs(taxonomy.Name + " Term")
                );

        }

        public void EditTaxonomy(TaxonomyPart taxonomy, string oldName) {        
            // rename term definition
            _contentDefinitionManager.AlterTypeDefinition(taxonomy.TermTypeName,
                cfg => cfg
                    .WithSetting("Taxonomy", taxonomy.Name)
                    .DisplayedAs(taxonomy.Name + " Term")
                );
        }

        public void DeleteTaxonomy(TaxonomyPart taxonomy) {
            _contentManager.Remove(taxonomy.ContentItem);

            // removing terms
            foreach (var term in GetTerms(taxonomy.Id)) {
                DeleteTerm(term);
            }

            _contentDefinitionManager.DeleteTypeDefinition(taxonomy.TermTypeName);
        }

        public string GenerateTermTypeName(string taxonomyName) {
            var disallowed = new Regex(@"[^\w]+");
            return disallowed.Replace(taxonomyName, "-");
        }

        public TermPart NewTerm(TaxonomyPart taxonomy) {
            var term = _contentManager.New<TermPart>(taxonomy.TermTypeName);
            term.TaxonomyId = taxonomy.Id;

            return term;
        }

        public IEnumerable<TermPart> GetTerms(int taxonomyId) {
            return _contentManager.Query<TermPart, TermPartRecord>()
                .Where(x => x.TaxonomyId == taxonomyId)
                .WithQueryHints(new QueryHints().ExpandRecords<AutoroutePartRecord, TitlePartRecord>())
                .List()
                .OrderBy(t => t);
        }

        public TermPart GetTermByPath(string path) {
            return _contentManager.Query<TermPart, TermPartRecord>()
                .Join<AutoroutePartRecord>()
                .WithQueryHints(new QueryHints().ExpandRecords<TitlePartRecord>())
                .Where(rr => rr.DisplayAlias == path)
                .List()
                .FirstOrDefault();
        }

        public IEnumerable<TermPart> GetAllTerms() {
            return _contentManager.Query<TermPart, TermPartRecord>().List().OrderBy(t => t);
        }

        public TermPart GetTerm(int id) {
            return _contentManager.Query<TermPart, TermPartRecord>().Where(x => x.Id == id).List().FirstOrDefault();
        }

        public IEnumerable<TermPart> GetTermsForContentItem(int contentItemId, string field = null) {
            return String.IsNullOrEmpty(field) 
                ? _termContentItemRepository.Fetch(x => x.TermsPartRecord.ContentItemRecord.Id == contentItemId).Select(t => GetTerm(t.TermRecord.Id)).OrderBy(t => t)
                : _termContentItemRepository.Fetch(x => x.TermsPartRecord.Id == contentItemId && x.Field == field).Select(t => GetTerm(t.TermRecord.Id)).OrderBy(t => t);
        }

        public TermPart GetTermByName(int taxonomyId, string name) {
            return _contentManager
                .Query<TermPart, TermPartRecord>()
                .Where(t => t.TaxonomyId == taxonomyId)
                .Join<TitlePartRecord>()
                .Where(r => r.Title == name)
                .List()
                .FirstOrDefault();
        }

        public void CreateTerm(TermPart termPart) {
            if (GetTermByName(termPart.TaxonomyId, termPart.Name) == null) {
                _authorizationService.CheckAccess(Permissions.CreateTerm, _services.WorkContext.CurrentUser, null);

                termPart.As<ICommonPart>().Container = GetTaxonomy(termPart.TaxonomyId).ContentItem;
                _contentManager.Create(termPart);
            }
            else {
                _notifier.Warning(T("The term {0} already exists in this taxonomy", termPart.Name));
            }
        }

        public void DeleteTerm(TermPart termPart) {
            _contentManager.Remove(termPart.ContentItem);

            foreach(var childTerm in GetChildren(termPart)) {
                _contentManager.Remove(childTerm.ContentItem);
            }

            // delete termContentItems
            var termContentItems = _termContentItemRepository
                .Fetch(t => t.TermRecord == termPart.Record)
                .ToList();

            foreach (var termContentItem in termContentItems) {
                _termContentItemRepository.Delete(termContentItem);
            }
        }

        public void DeleteAssociatedTerms(ContentItem contentItem) {
            var termContentItems = _termContentItemRepository
                .Fetch(t => t.TermsPartRecord.ContentItemRecord == contentItem.Record)
                .ToList();
            
            foreach(var termContentItem in termContentItems) {
                _termContentItemRepository.Delete(termContentItem);
            }
        }

        public void UpdateTerms(ContentItem contentItem, IEnumerable<TermPart> terms, string field) {
            var termsPart = contentItem.As<TermsPart>();

            // removing current terms for specific field
            var fieldIndexes = termsPart.Terms
                .Where(t => t.Field == field)
                .Select((t, i) => i)
                .OrderByDescending(i => i)
                .ToList();
            
            foreach(var x in fieldIndexes) {
                termsPart.Terms.RemoveAt(x);
            }
            
            // adding new terms list
            foreach(var term in terms) {
                termsPart.Terms.Add( 
                    new TermContentItem {
                        TermsPartRecord = termsPart.Record, 
                        TermRecord = term.Record, Field = field
                    });
            }
        }

        public IContentQuery<TermsPart, TermsPartRecord> GetContentItemsQuery(TermPart term, string fieldName = null)
        {
            var rootPath = term.FullPath + "/";

            var query = _contentManager
                .Query<TermsPart, TermsPartRecord>();

            if (String.IsNullOrWhiteSpace(fieldName)) {
                query = query.Where(
                    tpr => tpr.Terms.Any(tr =>
                        tr.TermRecord.Id == term.Id
                        || tr.TermRecord.Path.StartsWith(rootPath)));
            } else {
                query = query.Where(
                    tpr => tpr.Terms.Any(tr =>
                        tr.Field == fieldName
                         && (tr.TermRecord.Id == term.Id || tr.TermRecord.Path.StartsWith(rootPath))));
            }

            return query;
        }
        
        public long GetContentItemsCount(TermPart term, string fieldName = null) {
            return GetContentItemsQuery(term, fieldName).Count();
        }

        public IEnumerable<IContent> GetContentItems(TermPart term,
            int skip = 0,
            int count = 0,
            string fieldName = null)
        {

            return
                GetContentItemsQuery(term, fieldName)
                    .Join<CommonPartRecord>()
                    .OrderByDescending(x => x.CreatedUtc)
                    .Slice(skip, count);
        }

        public IEnumerable<TermPart> GetChildren(TermPart term){
            var rootPath = term.FullPath + "/";

            return _contentManager.Query<TermPart, TermPartRecord>()
                .Where(x => x.Path.StartsWith(rootPath))
                .List()
                .OrderBy(t => t);
        }

        public IEnumerable<TermPart> GetParents(TermPart term) {
            return term.Path.Split(new [] {'/'}, StringSplitOptions.RemoveEmptyEntries).Select(id => GetTerm(int.Parse(id)));
        }

        public IEnumerable<string> GetSlugs() {
            return _contentManager
                .Query<TaxonomyPart>()
                .Join<AutoroutePartRecord>()
                .List()
                .Select(t => t.Slug);
        }

        public IEnumerable<string> GetTermPaths() {
            return _contentManager
                .Query<TermPart>()
                .Join<AutoroutePartRecord>()
                .List()
                .Select(t => t.Slug);
        }

        public void MoveTerm(TaxonomyPart taxonomy, TermPart term, TermPart parentTerm) {
            var children = GetChildren(term);
            term.Container = parentTerm == null ? taxonomy.ContentItem : parentTerm.ContentItem;
            ProcessPath(term);

            var contentItem = _contentManager.Get(term.ContentItem.Id, VersionOptions.DraftRequired);
            _contentManager.Publish(contentItem);

            foreach (var childTerm in children) {
                ProcessPath(childTerm);

                contentItem = _contentManager.Get(childTerm.ContentItem.Id, VersionOptions.DraftRequired);
                _contentManager.Publish(contentItem);
            }
        }

        public void ProcessPath(TermPart term) {
            var parentTerm = term.Container.As<TermPart>();
            term.Path = parentTerm != null ? parentTerm.FullPath + "/": "/";
        }
    }
}
