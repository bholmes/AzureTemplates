using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Microsoft.WindowsAzure.MobileServices;
using System.Threading;

namespace ZUMOAPPNAME
{
	[Activity (MainLauncher = true, 
	           Icon="@drawable/ic_launcher", Label="@string/app_name",
	           Theme="@style/AppTheme")]
	public class ToDoActivity : Activity
	{
		//Mobile Service Client reference
		private MobileServiceClient mClient;

		//Mobile Service Table used to access data
		private IMobileServiceTable<ToDoItem> mToDoTable;

		//Adapter to sync the items list with the view
		private ToDoItemAdapter mAdapter;

		//EditText containing the "New ToDo" text
		private EditText mTextNewToDo;

		//Progress spinner to use for table operations
		private ProgressBar mProgressBar;

		const string applicationURL = @"https://mobilltasky.azure-mobile.net/";
		const string applicationKey = @"QFuPVQqUQNURoTUmsBCNkTJJTbumTe89";
		//		const string applicationURL = @"ZUMOAPPURL";
		//		const string applicationKey = @"ZUMOAPPKEY";

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Activity_To_Do);

			mProgressBar = (ProgressBar)FindViewById (Resource.Id.loadingProgressBar);

			// Initialize the progress bar
			mProgressBar.Visibility = ViewStates.Gone;

			// Create ProgressFilter to handle busy state
			var filter = new ProgressFilter ();
			filter.BusyStateChange += (busy) => {
				RunOnUiThread (() => {
					if (mProgressBar != null) 
						mProgressBar.Visibility = busy ? ViewStates.Visible : ViewStates.Gone;
				});
			};

			try {
				// Create the Mobile Service Client instance, using the provided
				// Mobile Service URL and key
				mClient = new MobileServiceClient (
					applicationURL,
					applicationKey).WithFilter (filter);

				// Get the Mobile Service Table instance to use
				mToDoTable = mClient.GetTable <ToDoItem> ();

				mTextNewToDo = (EditText)FindViewById (Resource.Id.textNewToDo);

				// Create an adapter to bind the items with the view
				mAdapter = new ToDoItemAdapter (this, Resource.Layout.Row_List_To_Do);
				ListView listViewToDo = (ListView)FindViewById (Resource.Id.listViewToDo);
				listViewToDo.Adapter = mAdapter;

				// Load the items from the Mobile Service
				RefreshItemsFromTable ();

			} catch (Java.Net.MalformedURLException e) {
				//TODO createAndShowDialog(new Exception("There was an error creating the Mobile Service. Verify the URL"), "Error");
			}

		}

		//Initializes the activity menu
		public override bool OnCreateOptionsMenu (IMenu menu)
		{
			this.MenuInflater.Inflate (Resource.Menu.activity_main, menu);
			return true;
		}

		//Select an option from the menu
		public override bool OnOptionsItemSelected (IMenuItem item)
		{
			if (item.ItemId == Resource.Id.menu_refresh) {
				RefreshItemsFromTable ();
			}
			return true;
		}

		//Refresh the list with the items in the Mobile Service Table
		async void RefreshItemsFromTable ()
		{

			try {
				// Get the items that weren't marked as completed and add them in the
				// adapter
				var list = await mToDoTable.Where (item => item.Complete == false).ToListAsync ();

				mAdapter.Clear ();

				foreach (ToDoItem current in list) {
					mAdapter.Add (current);
				}
			} catch (Exception e) {
				//TODO createAndShowDialog(exception, "Error");
			}
		}

		/// <summary>
		/// Mark an item as completed
		/// </summary>
		/// <param name="item">The item to mark</param>
		public async void CheckItem (ToDoItem item)
		{
			if (mClient == null) {
				return;
			}

			// Set the item as completed and update it in the table
			item.Complete = true;
			try {
				await mToDoTable.UpdateAsync (item);
				if (item.Complete) {
					mAdapter.Remove (item);
				}
			} catch (Exception e) {
				//TODO createAndShowDialog(exception, "Error");
			}
		}

		/// <summary>
		/// Add a new item
		/// </summary>
		/// <param name="view">The view that originated the call</param>
		[Java.Interop.Export()]
		public async void AddItem (View view)
		{
			if (mClient == null || string.IsNullOrWhiteSpace (mTextNewToDo.Text)) {
				return;
			}

			// Create a new item
			var item = new ToDoItem {
				Text = mTextNewToDo.Text,
				Complete = false
			};

			try {
				// Insert the new item
				await mToDoTable.InsertAsync (item);

				if (!item.Complete) {
					mAdapter.Add (item);
				}
			} catch (Exception e) {
				//TODO createAndShowDialog(exception, "Error");
			}

			mTextNewToDo.Text = "";
		}

		class ProgressFilter : IServiceFilter
		{
			long busyCount = 0;

			public event Action<bool> BusyStateChange;
			#region IServiceFilter implementation
			public System.Threading.Tasks.Task<IServiceFilterResponse> Handle (IServiceFilterRequest request, IServiceFilterContinuation continuation)
			{
				return HandleAsync (request, continuation);
			}

			public async System.Threading.Tasks.Task<IServiceFilterResponse> HandleAsync (IServiceFilterRequest request, IServiceFilterContinuation continuation)
			{
				if (Interlocked.Increment (ref this.busyCount) == 1 && this.BusyStateChange != null) {
					this.BusyStateChange (true);
				}

				var response = await continuation.Handle (request);

				if (Interlocked.Decrement (ref this.busyCount) == 0 && this.BusyStateChange != null) {
					this.BusyStateChange (false);
				}

				return response;
			}
			#endregion
		}
	}
}


