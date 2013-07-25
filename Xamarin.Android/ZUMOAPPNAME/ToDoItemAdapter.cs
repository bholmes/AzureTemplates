using System;
using Android.App;
using Android.Views;
using Android.Widget;


namespace ZUMOAPPNAME
{
	public class ToDoItemAdapter : ArrayAdapter<ToDoItem>
	{
		Activity activity;

		//Adapter View layout
		int layoutResourceId;

		public ToDoItemAdapter (Activity activity, int layoutResourceId) :
			base (activity, layoutResourceId)
		{
			this.activity = activity;
			this.layoutResourceId = layoutResourceId;
		}

		//Returns the view for a specific item on the list
		public override View GetView (int position, Android.Views.View convertView, Android.Views.ViewGroup parent)
		{
			var row = convertView;
			var currentItem = GetItem (position);
			CheckBox checkBox;

			if (row == null) {
				var inflater = activity.LayoutInflater;
				row = inflater.Inflate (layoutResourceId, parent, false);

				checkBox = row.FindViewById <CheckBox>(Resource.Id.checkToDoItem);

				checkBox.CheckedChange += async (sender, e) => {
					var cbSender = sender as CheckBox;
					if (cbSender != null && cbSender.Tag is ToDoItem && cbSender.Checked) {
						cbSender.Enabled = false;
						if (activity is ToDoActivity)
							await ((ToDoActivity)activity).CheckItem (cbSender.Tag as ToDoItem);
					}
				};
			}
			else
				checkBox = row.FindViewById <CheckBox>(Resource.Id.checkToDoItem);

			checkBox.Text = currentItem.Text;
			checkBox.Checked = false;
			checkBox.Enabled = true;
			checkBox.Tag = currentItem;

			return row;
		}
	}
}

