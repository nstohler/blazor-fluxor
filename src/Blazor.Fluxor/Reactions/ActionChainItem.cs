using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blazor.Fluxor.Reactions
{
	public class ActionChainItem
	{
		public object             Action         { get; set; }
		public ActionChainItem  Parent         { get; set; }
		public DateTime           ExpirationDate { get; set; }
		public List<ReactionItem> ReactionItems  { get; set; } = new List<ReactionItem>();

		public bool IsRoot()
		{
			return Parent == null;
		}

		public ActionChainItem GetRoot()
		{
			if (IsRoot())
			{
				return this;
			}

			return Parent.GetRoot();
		}

		public List<ActionChainItem> GetAncestors()
		{
			var results = new List<ActionChainItem>() {
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