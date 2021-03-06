using System;

using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Locations;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using PropertyFinder.Presenter;
using PropertyFinder.Model;

using Com.Actionbarsherlock.App;
using Com.Actionbarsherlock.View;

using IMenu = global::Com.Actionbarsherlock.View.IMenu;
using IMenuItem = global::Com.Actionbarsherlock.View.IMenuItem;
using MenuItem = global::Com.Actionbarsherlock.View.MenuItem;
using MenuInflater = global::Com.Actionbarsherlock.View.MenuInflater;

namespace com.propertycross.xamarin.android.Views
{
	[Activity (MainLauncher = true, WindowSoftInputMode = SoftInput.StateHidden, ScreenOrientation = ScreenOrientation.Portrait)]
	public class PropertyFinderView : SherlockActivity, PropertyFinderPresenter.View
	{
		private PropertyFinderPresenter presenter;
		private EditText searchText;
		private Button myLocationButton;
		private Button startSearchButton;
		private TextView messageText;
		private ListView recentSearchList;
		private RecentSearchAdapter adapter;
		private GeoLocationService geoLocationService;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			var app = (PropertyFinderApplication)Application;
			app.CurrentActivity = this;

			var uiMarshal = new MarshalInvokeService(app);
			var source = new PropertyDataSource(new JsonWebPropertySearch(uiMarshal));
			geoLocationService = new GeoLocationService((LocationManager)GetSystemService(Context.LocationService), uiMarshal);
			var stateService = new StatePersistenceService(app);
			PropertyFinderPersistentState state = stateService.LoadState();

			SetContentView (Resource.Layout.PropertyFinderView);
			searchText = (EditText) FindViewById(Resource.Id.search);
			searchText.TextChanged += SearchText_Changed;

			myLocationButton = (Button) FindViewById(Resource.Id.use_location);
			myLocationButton.Click += LocationButton_Clicked; 

			startSearchButton = (Button) FindViewById(Resource.Id.do_search);
			startSearchButton.Click += StartSearchButton_Clicked;

			messageText = (TextView) FindViewById(Resource.Id.mainview_message);

			recentSearchList = (ListView) FindViewById(Resource.Id.recentsearches_list);
			recentSearchList.ItemClick += RecentSearchItem_Clicked;
			adapter = new RecentSearchAdapter(this, new List<RecentSearch>());
			recentSearchList.Adapter = adapter;

			presenter = 
				new PropertyFinderPresenter(state,
				                            source,
				                            new NavigationService(app),
				                            geoLocationService);
			presenter.SetView(this);

			app.Presenter = presenter;
		}

		protected override void OnPause()
		{
			base.OnPause();
			geoLocationService.Unsubscribe();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			geoLocationService.Dispose();
		}

		public override bool OnCreateOptionsMenu(IMenu menu)
		{
			SupportMenuInflater.Inflate(Resource.Menu.favourites_view, menu);
			return true;
		}

		public override bool OnOptionsItemSelected(IMenuItem item)
		{
			if(item.ItemId == Resource.Id.favourites_view_item)
			{
				FavouritesClicked(this, EventArgs.Empty);
				return true;
			}
			else
			{
				return base.OnOptionsItemSelected(item);
			}
		}

		public string SearchText
		{
			set { searchText.SetText(value, TextView.BufferType.Editable); }
		}

		public void SetMessage(string msg)
		{
			// Ignore null messages from the presenter.
			if (msg != null)
			{
				messageText.Text = msg;
			}
		}

		public void DisplaySuggestedLocations (List<PropertyFinder.Model.Location> locations)
		{
		}

		public void DisplayRecentSearches(List<RecentSearch> recentSearches)
		{
			if(recentSearches != null)
			{
				adapter = new RecentSearchAdapter(this, recentSearches);
				recentSearchList.Adapter = adapter;
			}
		}

		public bool IsLoading
		{
			set
			{
				searchText.Enabled = !value;
				myLocationButton.Enabled = !value;
				startSearchButton.Enabled = !value;

				if (value)
				{
					messageText.Text = Resources.GetString(Resource.String.searching);
				}
				else
				{
					// Explicitly clear the message in the view.
					messageText.Text = null;
				}
			}
		}

		public event EventHandler SearchButtonClicked;
		public event EventHandler<SearchTextChangedEventArgs> SearchTextChanged;	
		public event EventHandler MyLocationButtonClicked;
		public event EventHandler FavouritesClicked;
		public event EventHandler<LocationSelectedEventArgs> LocationSelected;		
		public event EventHandler<RecentSearchSelectedEventArgs> RecentSearchSelected;

		private void SearchText_Changed(object sender, EventArgs e)
		{
			SearchTextChanged(this, new SearchTextChangedEventArgs(searchText.Text));
		}

		private void LocationButton_Clicked(object sender, EventArgs e)
		{
			MyLocationButtonClicked(this, EventArgs.Empty);
		}

		private void StartSearchButton_Clicked(object sender, EventArgs e)
		{
			SearchButtonClicked(this, EventArgs.Empty);
		}

		private void RecentSearchItem_Clicked(object sender, AdapterView.ItemClickEventArgs e)
		{
			RecentSearch item = adapter.GetItem(e.Position);
			RecentSearchSelected(this, new RecentSearchSelectedEventArgs(item));
		}
	}
}
