﻿using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using OfflineWorkflowSample.ViewModels;
using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace OfflineWorkflowSample
{
    public class PortalViewModel : ViewModelBase
    {
        public Dictionary<string, PortalFolderViewModel> Folders { get; } = new Dictionary<string, PortalFolderViewModel>();
        public Dictionary<string, PortalFolderViewModel> Groups { get; } = new Dictionary<string, PortalFolderViewModel>();

        public List<PortalFolderViewModel> VisibleFolders => Folders.Values.Where(folder => folder.SectionHasContent).ToList();
        public List<PortalFolderViewModel> VisibleGroups => Groups.Values.Where(group => group.SectionHasContent).ToList();

        public PortalSearchViewModel SearchViewModel { get; } = new PortalSearchViewModel();

        private List<Basemap> _orgBasemaps = new List<Basemap>();

        private List<Basemap> _defaultBasemaps = new List<Basemap>
        {
            Basemap.CreateImagery(),
            Basemap.CreateImageryWithLabels(),
            Basemap.CreateLightGrayCanvas(),
            Basemap.CreateNationalGeographic(),
            Basemap.CreateOceans(),
            Basemap.CreateOpenStreetMap(),
            Basemap.CreateStreets()
        };

        public List<Basemap> OrgBasemaps
        {
            get
            {
                if (!_orgBasemaps.Any())
                {
                    return _defaultBasemaps;
                }

                return _orgBasemaps;
            }
        }

        private PortalFolderViewModel _selectedFolder;

        public PortalFolderViewModel SelectedFolder
        {
            get => _selectedFolder;
            set => SetProperty(ref _selectedFolder, value);
        }

        private PortalFolderViewModel _selectedGroup;

        public PortalFolderViewModel SelectedGroup
        {
            get => _selectedGroup;
            set => SetProperty(ref _selectedGroup, value);
        }

        public ArcGISPortal Portal { get; set; }

        public async Task LoadPortalAsync(ArcGISPortal portal)
        {
            Portal = portal;

            SearchViewModel.Initialize(portal);

            try
            {
                // Get 'featured content'
                var featuredItems = await portal.GetFeaturedItemsAsync();
                Folders["Featured content"] = new PortalFolderViewModel("Featured", featuredItems.ToList());
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                // Ignore
            }

            try
            {
                // Get the 'my content' group
                var userContent = await portal.User.GetContentAsync();
                Folders["All my content"] = new PortalFolderViewModel("All my content", userContent.Items.ToList());

                // Get all other folders
                foreach (PortalFolder folder in userContent.Folders)
                {
                    var itemsForFolder = await portal.User.GetContentAsync(folder.FolderId);
                    Folders[folder.Title] = new PortalFolderViewModel(folder.Title, itemsForFolder.ToList());
                }

                // Get the groups
                foreach (var item in portal.User.Groups)
                {
                    PortalQueryParameters parameters = PortalQueryParameters.CreateForItemsInGroup(item.GroupId);
                    var itemResults = await portal.FindItemsAsync(parameters);
                    // TO-DO - update for query pagination
                    Groups[item.Title] = new PortalFolderViewModel(item.Title, itemResults.Results.ToList());
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                // Ignore
            }

            try
            {
                // Get the basemaps.
                _orgBasemaps.Clear();
                _orgBasemaps.AddRange(await Portal.GetBasemapsAsync());
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                // Ignore
            }

            // Set the initial selections.
            SelectedFolder = Folders.Values.FirstOrDefault();
            SelectedGroup = Groups.Values.FirstOrDefault();
        }

        // Is this a good idea?
        private string _searchFilter;

        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                SetProperty(ref _searchFilter, value);

                foreach (PortalFolderViewModel container in Folders.Values.Concat(Groups.Values))
                {
                    container.SearchFilter = value;
                }

                HandleFilterChangesForFolders();
            }
        }

        private bool _offlineOnlyFilter;

        public bool OfflineOnlyFilter
        {
            get => _offlineOnlyFilter;
            set
            {
                SetProperty(ref _offlineOnlyFilter, value);

                foreach (PortalFolderViewModel container in Folders.Values.Concat(Groups.Values))
                {
                    container.OfflineOnlyFilter = OfflineOnlyFilter;
                }

                HandleFilterChangesForFolders();
            }
        }

        private PortalItemType? _typeFilter;

        public PortalItemType? TypeFilter
        {
            get => _typeFilter;
            set
            {
                SetProperty(ref _typeFilter, value);

                foreach (PortalFolderViewModel container in Folders.Values.Concat(Groups.Values))
                {
                    container.TypeFilter = value;
                }

                HandleFilterChangesForFolders();
            }
        }

        public List<PortalItemType?> AvailableTypeFilters => _availableTypeFilters;

        private static List<PortalItemType?> _availableTypeFilters = new List<PortalItemType?>
        {
            null,
            PortalItemType.WebMap,
            PortalItemType.WebScene,
            PortalItemType.MobileMapPackage
        };

        private void HandleFilterChangesForFolders()
        {
            RaisePropertyChanged(nameof(VisibleFolders));
            RaisePropertyChanged(nameof(VisibleGroups));

            // Set the initial selections now that visible folders have changed.
            SelectedFolder = VisibleFolders.FirstOrDefault();
            SelectedGroup = VisibleGroups.FirstOrDefault();
        }
    }

    public class PortalFolderViewModel : ViewModelBase
    {
        private string _searchFilter;
        private PortalItemType? _typeFilter;
        private List<PortalItem> _allItems;
        private bool _offlineOnly;

        public string Title { get; }

        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                SetProperty(ref _searchFilter, value);
                RaisePropertyChanged(nameof(Items));
                RaisePropertyChanged(nameof(SectionHasContent));
            }
        }

        public PortalItemType? TypeFilter
        {
            get => _typeFilter;
            set
            {
                SetProperty(ref _typeFilter, value);
                RaisePropertyChanged(nameof(Items));
                RaisePropertyChanged(nameof(SectionHasContent));
            }
        }

        public bool OfflineOnlyFilter
        {
            get => _offlineOnly;
            set
            {
                SetProperty(ref _offlineOnly, value);
                RaisePropertyChanged(nameof(Items));
                RaisePropertyChanged(nameof(SectionHasContent));
            }
        }

        public IEnumerable<PortalItem> Items
        {
            get
            {
                IEnumerable<PortalItem> items = _allItems;
                if (!String.IsNullOrWhiteSpace(SearchFilter))
                {
                    items = items.Where(item => item.Title.Contains(SearchFilter));
                }

                if (TypeFilter != null)
                {
                    items = items.Where(item => item.Type == TypeFilter);
                }

                if (_offlineOnly)
                {
                    items = items.Where(item => item.TypeKeywords.Contains("Offline"));
                }

                return items;
            }
        }

        public bool SectionHasContent => Items.Any();

        public PortalFolderViewModel(string title, List<PortalItem> items)
        {
            _allItems = items;
            Title = title;
        }
    }
}