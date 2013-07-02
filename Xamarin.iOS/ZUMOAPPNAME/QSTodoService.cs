using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure.MobileServices;
using MonoTouch.Foundation;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace ZUMOAPPNAME
{
	public class TodoItem
	{
		public int Id { get; set; }

		[DataMember (Name = "text")]
		public string Text { get; set; }

		[DataMember (Name = "complete")]
		public bool Complete { get; set; }
	}

	public class QSTodoService
	{
		static QSTodoService instance = new QSTodoService ();
		const string applicationURL = @"https://mobilltasky.azure-mobile.net/";
		const string applicationKey = @"QFuPVQqUQNURoTUmsBCNkTJJTbumTe89";
		//		const string applicationURL = @"ZUMOAPPURL";
		//		const string applicationKey = @"ZUMOAPPKEY";
		MobileServiceClient client;
		IMobileServiceTable<TodoItem> todoTable;
		private List<TodoItem> items;
		int busyCount = 0;

		public event Action<bool> BusyUpdate;

		QSTodoService ()
		{
			// Initialize the Mobile Service client with your URL and key
			this.client = new MobileServiceClient (applicationURL, applicationKey);

			// Create an MSTable instance to allow us to work with the TodoItem table
			this.todoTable = this.client.GetTable <TodoItem> ();
		}

		public static QSTodoService DefaultService {
			get {
				return instance;
			}
		}

		public List<TodoItem> Items {
			get {
				return items;
			}
		}

		async public Task<List<TodoItem>> RefreshDataAsync ()
		{
			try {
				Busy (true);
				// This code refreshes the entries in the list view by querying the TodoItems table.
				// The query excludes completed TodoItems
				items = await todoTable
					.Where (todoItem => todoItem.Complete == false).ToListAsync ();
			} catch (MobileServiceInvalidOperationException e) {
				Console.Error.WriteLine (@"ERROR {0}", e.Message);
				return null;
			} finally {
				Busy (false);
			}

			return items;
		}

		public async Task InsertTodoItemAsync (TodoItem todoItem)
		{
			try {
				Busy (true);
				// This code inserts a new TodoItem into the database. When the operation completes
				// and Mobile Services has assigned an Id, the item is added to the CollectionView
				await todoTable.InsertAsync (todoItem);
				items.Add (todoItem); 
			} catch (MobileServiceInvalidOperationException e) {
				Console.Error.WriteLine (@"ERROR {0}", e.Message);
			} finally {
				Busy (false);
			}
		}

		public async Task CompleteItemAsync (TodoItem item)
		{
			try {
				Busy (true);
				// This code takes a freshly completed TodoItem and updates the database. When the MobileService 
				// responds, the item is removed from the list 
				item.Complete = true;
				await todoTable.UpdateAsync (item);
				items.Remove (item);
			} catch (MobileServiceInvalidOperationException e) {
				Console.Error.WriteLine (@"ERROR {0}", e.Message);
			} finally {
				Busy (false);
			}
		}

		void Busy (bool busy)
		{
			// assumes always executes on UI thread
			if (busy) {
				if (this.busyCount == 0 && this.BusyUpdate != null) {
					this.BusyUpdate (true);
				}
				this.busyCount ++;
			} else {
				if (this.busyCount == 1 && this.BusyUpdate != null) {
					this.BusyUpdate (false);
				}
				this.busyCount--;
			}
		}
	}
}

