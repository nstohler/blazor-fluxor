using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blazor.Fluxor.Reactions
{
	public class ActionHistoryItem
	{
		public object             Action         { get; set; }
		public ActionHistoryItem  Parent         { get; set; }
		public DateTime           ExpirationDate { get; set; }
		public List<ReactionItem> ReactionItems  { get; set; } = new List<ReactionItem>();

		public bool IsRoot()
		{
			return Parent == null;
		}

		public ActionHistoryItem GetRoot()
		{
			if (IsRoot())
			{
				return this;
			}

			return Parent.GetRoot();
		}

		public List<ActionHistoryItem> GetAncestors()
		{
			var results = new List<ActionHistoryItem>() {
				this
			};
			if (!IsRoot())
			{
				results.AddRange(Parent.GetAncestors());
			}

			return results;
		}
	}
}