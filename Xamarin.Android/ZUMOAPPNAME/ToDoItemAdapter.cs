using System;
using Android.App;
using Android.Views;
using Android.Widget;


namespace ZUMOAPPNAME
{
	public class ToDoItemAdapter : ArrayAdapter<ToDoItem>
	{
		Activity mContext;

		//Adapter View layout
		int mLayoutResourceId;

		public ToDoItemAdapter (Activity context, int layoutResourceId) :
			base (context, layoutResourceId)
		{
			mContext = context;
			mLayoutResourceId = layoutResourceId;
		}

		//Returns the view for a specific item on the list
		public override View GetView (int position, Android.Views.View convertView, Android.Views.ViewGroup parent)
		{
			var row = convertView;

			ToDoItem currentItem = this.GetItem (position);

			if (row == null) {
				var inflater = mContext.LayoutInflater;
				row = inflater.Inflate (mLayoutResourceId, parent, false);
			}

			row.Tag = currentItem;
			CheckBox checkBox = (CheckBox)row.FindViewById (Resource.Id.checkToDoItem);
			checkBox.Text = currentItem.Text;
			checkBox.Checked = false;
			checkBox.Enabled = true;

			checkBox.SetOnCheckedChangeListener (new OnCheckedChangeWrapper ((buttonView, isChecked) => {
				if (checkBox.Checked) {
					if (mContext is ToDoActivity) {
						((ToDoActivity)mContext).CheckItem (currentItem);
					}
				}
			}));

			return row;
		}

		class OnCheckedChangeWrapper : Java.Lang.Object, CompoundButton.IOnCheckedChangeListener
		{
			Action <CompoundButton, bool> callback;

			public OnCheckedChangeWrapper (Action <CompoundButton, bool> callback)
			{
				this.callback = callback;
			}

			#region IOnCheckedChangeListener implementation
			public void OnCheckedChanged (CompoundButton buttonView, bool isChecked)
			{
				this.callback (buttonView, isChecked);
			}
			#endregion
		}
	}
}

