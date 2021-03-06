using System;
using Android.OS;
using Android.App;
using Android.Views;
using Android.Widget;
using Microsoft.WindowsAzure.MobileServices;
using System.Threading.Tasks;


namespace ZUMOAPPNAME
{
	[Activity (MainLauncher = true, 
	           Icon="@drawable/ic_launcher", Label="@string/app_name",
	           Theme="@style/AppTheme")]
	public class ToDoActivity : Activity
	{
		//Mobile Service Client reference
		private MobileServiceClient client;

		//Mobile Service Table used to access data
		private IMobileServiceTable<ToDoItem> toDoTable;

		//Adapter to sync the items list with the view
		private ToDoItemAdapter adapter;

		//EditText containing the "New ToDo" text
		private EditText textNewToDo;

		//Progress spinner to use for table operations
		private ProgressBar progressBar;

		const string applicationURL = @"ZUMOAPPURL";
		const string applicationKey = @"ZUMOAPPKEY";

		protected override async void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Activity_To_Do);

			progressBar = FindViewById<ProgressBar> (Resource.Id.loadingProgressBar);

			// Initialize the progress bar
			progressBar.Visibility = ViewStates.Gone;

			// Create ProgressFilter to handle busy state
			var filter = new ProgressFilter ();
			filter.BusyStateChange += (busy) => {
				if (progressBar != null) 
					progressBar.Visibility = busy ? ViewStates.Visible : ViewStates.Gone;
			};

			try {
				// Create the Mobile Service Client instance, using the provided
				// Mobile Service URL and key
				client = new MobileServiceClient (
					applicationURL,
					applicationKey).WithFilter (filter);

				// Get the Mobile Service Table instance to use
				toDoTable = client.GetTable <ToDoItem> ();

				textNewToDo = FindViewById<EditText> (Resource.Id.textNewToDo);

				// Create an adapter to bind the items with the view
				adapter = new ToDoItemAdapter (this, Resource.Layout.Row_List_To_Do);
				var listViewToDo = FindViewById<ListView> (Resource.Id.listViewToDo);
				listViewToDo.Adapter = adapter;

				// Load the items from the Mobile Service
				await RefreshItemsFromTableAsync ();

			} catch (Java.Net.MalformedURLException) {
				CreateAndShowDialog (new Exception ("There was an error creating the Mobile Service. Verify the URL"), "Error");
			} catch (Exception e) {
				CreateAndShowDialog (e, "Error");
			}
		}

		//Initializes the activity menu
		public override bool OnCreateOptionsMenu (IMenu menu)
		{
			MenuInflater.Inflate (Resource.Menu.activity_main, menu);
			return true;
		}

		//Select an option from the menu
		public override bool OnOptionsItemSelected (IMenuItem item)
		{
			if (item.ItemId == Resource.Id.menu_refresh) {
				OnRefreshItemsSelected ();
			}
			return true;
		}

		// Called when the refresh menu opion is selected
		async void OnRefreshItemsSelected ()
		{
			await RefreshItemsFromTableAsync ();
		}

		//Refresh the list with the items in the Mobile Service Table
		async Task RefreshItemsFromTableAsync ()
		{
			try {
				// Get the items that weren't marked as completed and add them in the
				// adapter
				var list = await toDoTable.Where (item => item.Complete == false).ToListAsync ();

				adapter.Clear ();

				foreach (ToDoItem current in list)
					adapter.Add (current);

			} catch (Exception e) {
				CreateAndShowDialog (e, "Error");
			}
		}

		public async Task CheckItem (ToDoItem item)
		{
			if (client == null) {
				return;
			}

			// Set the item as completed and update it in the table
			item.Complete = true;
			try {
				await toDoTable.UpdateAsync (item);
				if (item.Complete)
					adapter.Remove (item);

			} catch (Exception e) {
				CreateAndShowDialog (e, "Error");
			}
		}

		[Java.Interop.Export()]
		public async void AddItem (View view)
		{
			if (client == null || string.IsNullOrWhiteSpace (textNewToDo.Text)) {
				return;
			}

			// Create a new item
			var item = new ToDoItem {
				Text = textNewToDo.Text,
				Complete = false
			};

			try {
				// Insert the new item
				await toDoTable.InsertAsync (item);

				if (!item.Complete) {
					adapter.Add (item);
				}
			} catch (Exception e) {
				CreateAndShowDialog (e, "Error");
			}

			textNewToDo.Text = "";
		}

		void CreateAndShowDialog (Exception exception, String title)
		{
			CreateAndShowDialog (exception.Message, title);
		}

		void CreateAndShowDialog (string message, string title)
		{
			AlertDialog.Builder builder = new AlertDialog.Builder (this);

			builder.SetMessage (message);
			builder.SetTitle (title);
			builder.Create ().Show ();
		}

		class ProgressFilter : IServiceFilter
		{
			int busyCount = 0;

			public event Action<bool> BusyStateChange;
			
			#region IServiceFilter implementation
			public async System.Threading.Tasks.Task<IServiceFilterResponse> Handle (IServiceFilterRequest request, IServiceFilterContinuation continuation)
			{
				// assumes always executes on UI thread
				if (busyCount++ == 0 && BusyStateChange != null)
					BusyStateChange (true);

				var response = await continuation.Handle (request);

				// assumes always executes on UI thread
				if (--busyCount == 0 && BusyStateChange != null)
					BusyStateChange (false);

				return response;
			}

			#endregion
		}
	}
}


