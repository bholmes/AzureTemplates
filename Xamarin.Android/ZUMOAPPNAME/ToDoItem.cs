using System;
using System.Runtime.Serialization;

namespace ZUMOAPPNAME
{
	public class ToDoItem : Java.Lang.Object
	{
		public int Id { get; set; }

		[DataMember (Name = "text")]
		public string Text { get; set; }

		[DataMember (Name = "complete")]
		public bool Complete { get; set; }
	}
}

