﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blazor.Fluxor.Reactions
{
	public class ActionHistoryItem
	{
		//public ActionHistoryItem Root { get; set; }
		public ActionHistoryItem Parent { get; set; }

		public object   Action         { get; set; }
		public DateTime ExpirationDate     { get; set; }

		public List<ReactionItem> ReactionItems { get; set; }
		// public bool               Invoked       { get; set; }

		//public ActionHistoryItem GetRoot()
		//{
		//	if (this.Root != null)
		//	{
		//		return this;
		//	}
		//}
		public ActionHistoryItem GetRoot()
		{
			if (Parent == null)
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
			if (Parent != null)
			{
				results.AddRange(Parent.GetAncestors());
			}
			return results;
		}
	}
}