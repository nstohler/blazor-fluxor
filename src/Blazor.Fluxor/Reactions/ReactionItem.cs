using System;

namespace Blazor.Fluxor.Reactions
{
	public class ReactionItem
	{
		public Action<object> Action     { get; set; }
		public Type           ActionType { get; set; }

		public DateTime TimeStamp { get; set; }
		public bool     Invoked   { get; set; }

		public ReactionItem Clone()
		{
			return new ReactionItem() {
				Action     = this.Action,
				ActionType = this.ActionType,
				TimeStamp  = TimeStamp, 
			};
		}
	}
}