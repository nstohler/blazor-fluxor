using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Blazor.Fluxor.Reactions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Blazor.Fluxor
{
	/// <see cref="IStore"/>
	public class Store : IStore
	{
		/// <see cref="IStore.Features"/>
		public IReadOnlyDictionary<string, IFeature> Features => FeaturesByName;

		/// <see cref="IStore.Initialized"/>
		public Task Initialized => InitializedCompletionSource.Task;

		private IStoreInitializationStrategy StoreInitializationStrategy;

		private readonly Dictionary<string, IFeature> FeaturesByName =
			new Dictionary<string, IFeature>(StringComparer.InvariantCultureIgnoreCase);

		private readonly List<IEffect>     Effects             = new List<IEffect>();
		private readonly List<IMiddleware> Middlewares         = new List<IMiddleware>();
		private readonly List<IMiddleware> ReversedMiddlewares = new List<IMiddleware>();

		private readonly Queue<object> QueuedActions = new Queue<object>();

		//private readonly Dictionary<object, IHasReaction> ActionReactionDict = new Dictionary<object, IHasReaction>();

		// TODO: simple list of ReactionEntry
		private readonly List<ReactionEntry> ActionHistory = new List<ReactionEntry>();

		// reactions
		//private readonly Dictionary<Type, ReactionItem> TypeReactionItemDict = new Dictionary<Type, ReactionItem>();

		//private readonly Dictionary<string, Dictionary<Type, ReactionItem>> GuidTypeReactionItemDict =
		//	new Dictionary<string, Dictionary<Type, ReactionItem>>();

		//private readonly Dictionary<IHasReaction, Dictionary<Type, ReactionItem>> ActionTypeReactionItemDict =
		//	new Dictionary<IHasReaction, Dictionary<Type, ReactionItem>>();

		private readonly TaskCompletionSource<bool> InitializedCompletionSource = new TaskCompletionSource<bool>();

		private int                      BeginMiddlewareChangeCount;
		private bool                     HasActivatedStore;
		private bool                     IsInsideMiddlewareChange => BeginMiddlewareChangeCount > 0;
		private Action<IFeature, object> IFeatureReceiveDispatchNotificationFromStore;

		/// <summary>
		/// Creates an instance of the store
		/// </summary>
		/// <param name="storeInitializationStrategy">The strategy used to initialise the store</param>
		public Store(IStoreInitializationStrategy storeInitializationStrategy)
		{
			StoreInitializationStrategy = storeInitializationStrategy;

			MethodInfo dispatchNotifictionFromStoreMethodInfo =
				typeof(IFeature)
					.GetMethod(nameof(IFeature.ReceiveDispatchNotificationFromStore));
			IFeatureReceiveDispatchNotificationFromStore = (Action<IFeature, object>)
				Delegate.CreateDelegate(typeof(Action<IFeature, object>), dispatchNotifictionFromStoreMethodInfo);

			Dispatch(new StoreInitializedAction());
		}

		/// <see cref="IStore.AddFeature(IFeature)"/>
		public void AddFeature(IFeature feature)
		{
			if (feature == null)
				throw new ArgumentNullException(nameof(feature));

			FeaturesByName.Add(feature.GetName(), feature);
		}

		public void DispatchReaction(object baseAction, object reaction)
		{
			// reaction dispatched as action, action is "reacts-to-action"
			Dispatch<object, object, object>(reaction, baseAction, null, null, null);
		}

		public void Dispatch(object action)
		{
			Dispatch<object, object, object>(action, null, null, null, null);
		}

		/// <see cref="IDispatcher.Dispatch"/>
		//public void Dispatch<T>(object action, Action<IResultAction<T>> resultAction)
		public void Dispatch<T>(object action, Action<T> resultAction)
		{
			Dispatch<T, object, object>(action, null, resultAction, null, null);
		}

		public void Dispatch<T1, T2>(object action, Action<T1> resultAction1, Action<T2> resultAction2)
		{
			Dispatch<T1, T2, object>(action, null, resultAction1, resultAction2, null);
		}

		public void Dispatch<T1, T2, T3>(
			object action, object baseAction,
			Action<T1> resultAction1, Action<T2> resultAction2, Action<T3> resultAction3)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			// Do not allow task dispatching inside a middleware-change.
			// These change cycles are for things like "jump to state" in Redux Dev Tools
			// and should be short lived.
			// We avoid dispatching inside a middleware change because we don't want UI events (like component Init)
			// that trigger actions (such as fetching data from a server) to execute
			if (IsInsideMiddlewareChange)
				return;

			// If there was already an action in the Queue then an action dispatch is already in progress, so we will just
			// let this new action be added to the queue and then exit
			// Note: This is to cater for the following scenario
			//	1: An action is dispatched
			//	2: An effect is triggered
			//	3: The effect immediately dispatches a new action
			// The Queue ensures it is processed after its triggering action has completed rather than immediately
			bool wasAlreadyDispatching = QueuedActions.Any();

			//QueuedActions.Enqueue((action, resultAction));
			QueuedActions.Enqueue(action);

			////if(baseAction == null)
			//{
			//	ActionHistory.Add(new ReactionEntry() {
			//		RootAction = action,
			//		BaseAction = action,
			//		Action     = action,
			//		DateTime   = DateTime.UtcNow
			//	});
			//}

			// configure reaction in queue
			//if (baseAction is IHasReaction baseActionCasted)
			//{
			//	//ActionReactionDict.Add(baseAction, action);
			//	ActionReactionDict.Add(action, baseActionCasted);

			//	//// TODO: check if baseAction is already linked to another baseAction?

			//	//var rootAction = ActionHistory.LastOrDefault(x => x.RootAction == baseAction) ?? baseAction;

			//	//ActionHistory.Add(new ReactionEntry() {
			//	//	RootAction = rootAction,
			//	//	BaseAction = baseActionCasted,
			//	//	Action     = action,
			//	//	DateTime   = DateTime.UtcNow
			//	//});

			//	//if (rootAction != baseAction)
			//	//{
			//	//	ActionReactionDict.Add(action, (IHasReaction)rootAction);
			//	//}
			//}

			// prepare reaction items
			var reactionItems = GetReactionItems(resultAction1, resultAction2, resultAction3);

			//var root = ActionHistory.LastOrDefault(x => x.)

			if (baseAction == null)
			{
				ActionHistory.Add(new ReactionEntry() {
					//Root          = null,
					Parent        = null,
					Action        = action,
					DateTime      = DateTime.UtcNow,
					ReactionItems = reactionItems
				});
			}
			else
			{
				var parent = ActionHistory.LastOrDefault(x => x.Action == baseAction);
				//var root   = parent.GetRoot();

				// add current item
				ActionHistory.Add(new ReactionEntry() {
					//Root          = root,
					Parent        = parent,
					Action        = action,
					DateTime      = DateTime.UtcNow,
					// ReactionItems = reactionItems
				});
			}


			// store reactionItems

			//// with guid => GuidTypeReactionItemDict
			//Console.WriteLine("configure reaction...");
			//if (action is ActionWithReactionBase actionWithReaction)
			//{
			//	var guidKey = Guid.NewGuid().ToString();
			//	actionWithReaction.ActionGuid = guidKey;

			//	foreach (var reactionItem in reactionItems)
			//	{
			//		if (!GuidTypeReactionItemDict.ContainsKey(guidKey))
			//		{
			//			GuidTypeReactionItemDict.Add(guidKey, new Dictionary<Type, ReactionItem>());
			//		}

			//		var typeReactionDict = GuidTypeReactionItemDict[guidKey];

			//		var actionTypeKey = reactionItem.ActionType;
			//		if (!typeReactionDict.ContainsKey(actionTypeKey))
			//		{
			//			typeReactionDict.Add(actionTypeKey, reactionItem);
			//		}
			//		else
			//		{
			//			throw new NotImplementedException("no multiple entries of same types");
			//		}
			//	}
			//}

			//// IHasReaction only => ActionTypeReactionItemDict
			//if (action is IHasReaction actionKey)
			//{
			//	//Dictionary<Type, ReactionItem> typeReactionDict = null;

			//	foreach (var reactionItem in reactionItems)
			//	{
			//		if (!ActionTypeReactionItemDict.ContainsKey(actionKey))
			//		{
			//			ActionTypeReactionItemDict.Add(actionKey, new Dictionary<Type, ReactionItem>());
			//		}

			//		//var typeReactionDict = ActionTypeReactionItemDict[actionKey];
			//		var typeReactionDict = ActionTypeReactionItemDict[actionKey];

			//		var actionTypeKey = reactionItem.ActionType;
			//		if (!typeReactionDict.ContainsKey(actionTypeKey))
			//		{
			//			typeReactionDict.Add(actionTypeKey, reactionItem);
			//		}
			//		else
			//		{
			//			throw new NotImplementedException("no multiple entries of same types");
			//		}

			//		//// clone reactionItems for subReactions
			//		//Console.WriteLine(
			//		//	$"History contains: {string.Join(" | ", ActionHistory.Select(x => x.Action.GetType().ToString()))}");

			//		//var baseActionEntry = ActionHistory.LastOrDefault(x => x.RootAction == baseAction);
			//		//if (baseActionEntry != null && baseActionEntry.RootAction is IHasReaction baseActionKey)
			//		//{
			//		//	Console.WriteLine($"-- extending dict {baseActionKey.GetType().Name}");

			//		//	if (!ActionTypeReactionItemDict.ContainsKey(baseActionKey))
			//		//	{
			//		//		ActionTypeReactionItemDict.Add(baseActionKey, new Dictionary<Type, ReactionItem>());
			//		//	}

			//		//	//var typeReactionDict = ActionTypeReactionItemDict[actionKey];
			//		//	var base_TypeReactionDict = ActionTypeReactionItemDict[baseActionKey];

			//		//	var baseActionTypeKey = reactionItem.ActionType;
			//		//	if (!base_TypeReactionDict.ContainsKey(baseActionTypeKey))
			//		//	{
			//		//		base_TypeReactionDict.Add(actionTypeKey, reactionItem);
			//		//	}
			//		//	else
			//		//	{
			//		//		throw new NotImplementedException("no multiple entries of same types");
			//		//	}

			//		//	//if (ActionTypeReactionItemDict.TryGetValue(baseActionKey, out Dictionary<Type, ReactionItem> typeReactionItemDict))
			//		//	//{
			//		//	//	Console.WriteLine($"-- extending dict");
			//		//	//	//ActionTypeReactionItemDict.Add(baseActionKey, typeReactionItemDict);
			//		//	//	//var typeReactionDict = ActionTypeReactionItemDict[actionKey];
			//		//	//	//typeReactionDict.Add(baseActionKey, typeReactionItemDict);
			//		//	//	ActionTypeReactionItemDict[baseActionKey] = typeReactionItemDict;
			//		//	//}
			//		//}
			//	}

			//	// dump dict?
			//}

			if (wasAlreadyDispatching)
				return;

			// HasActivatedStore is set to true when the page finishes loading
			// At which point DequeueActions will be called
			if (!HasActivatedStore)
				return;

			DequeueActions();
		}

		private List<ReactionItem> GetReactionItems<T1, T2, T3>(Action<T1> resultAction1, Action<T2> resultAction2,
			Action<T3> resultAction3)
		{
			var resultActions = new List<ReactionItem>();

			var guid      = Guid.NewGuid();
			var timestamp = DateTime.Now;

			if (resultAction1 != null)
			{
				resultActions.Add(CreateReactionItem(resultAction1, timestamp, guid));
			}

			if (resultAction2 != null)
			{
				resultActions.Add(CreateReactionItem(resultAction2, timestamp, guid));
			}

			if (resultAction3 != null)
			{
				resultActions.Add(CreateReactionItem(resultAction3, timestamp, guid));
			}

			return resultActions;
		}

		private ReactionItem CreateReactionItem<T>(Action<T> action, DateTime timestamp, Guid guid)
		{
			return new ReactionItem() {
				Action     = Convert(action),
				ActionType = typeof(T),
				Guid       = guid,
				TimeStamp  = timestamp
			};
		}

		public Action<object> Convert<T>(Action<T> myActionT)
		{
			if (myActionT == null) return null;
			else return new Action<object>(o => myActionT((T)o));
		}

		/// <see cref="IStore.AddEffect(IEffect)"/>
		public void AddEffect(IEffect effect)
		{
			if (effect == null)
				throw new ArgumentNullException(nameof(effect));
			Effects.Add(effect);
		}

		/// <see cref="IStore.AddMiddleware(IMiddleware)"/>
		public void AddMiddleware(IMiddleware middleware)
		{
			Middlewares.Add(middleware);
			ReversedMiddlewares.Insert(0, middleware);
			// Initialize the middleware immediately if the store has already been initialized, otherwise this will be
			// done the first time Dispatch is called
			if (HasActivatedStore)
			{
				middleware.Initialize(this);
				middleware.AfterInitializeAllMiddlewares();
			}
		}

		/// <see cref="IStore.BeginInternalMiddlewareChange"/>
		public IDisposable BeginInternalMiddlewareChange()
		{
			BeginMiddlewareChangeCount++;
			IDisposable[] disposables = Middlewares
				.Select(x => x.BeginInternalMiddlewareChange())
				.ToArray();
			return new DisposableCallback(() =>
			{
				BeginMiddlewareChangeCount--;
				if (BeginMiddlewareChangeCount == 0)
					disposables.ToList().ForEach(x => x.Dispose());
			});
		}

		/// <see cref="IStore.Initialize"/>
		public RenderFragment Initialize()
		{
			if (HasActivatedStore)
				return builder => { };

			StoreInitializationStrategy.Initialize(ActivateStore);
			return (RenderTreeBuilder renderer) =>
			{
				var scriptBuilder = new StringBuilder();
				scriptBuilder.AppendLine("if (!window.fluxorInitialized) {");
				{
					scriptBuilder.AppendLine("window.fluxorInitialized = true;");
					foreach (IMiddleware middleware in Middlewares)
					{
						string middlewareScript = middleware.GetClientScripts();
						if (middlewareScript != null)
						{
							scriptBuilder.AppendLine($"// Middleware scripts: {middleware.GetType().FullName}");
							scriptBuilder.AppendLine($"{middlewareScript}");
						}
					}
				}
				scriptBuilder.AppendLine("}");

				string script = scriptBuilder.ToString();
				renderer.OpenElement(1, "script");
				renderer.AddAttribute(2, "id", "initializeFluxor");
				renderer.AddMarkupContent(3, script);
				renderer.CloseElement();
			};
		}

		private void TriggerEffects(object action)
		{
			var effectsToTrigger = Effects.Where(x => x.ShouldReactToAction(action));
			foreach (var effect in effectsToTrigger)
				effect.HandleAsync(action, this);
		}

		private void InitializeMiddlewares()
		{
			Middlewares.ForEach(x => x.Initialize(this));
			Middlewares.ForEach(x => x.AfterInitializeAllMiddlewares());
		}

		private void ExecuteMiddlewareBeforeDispatch(object actionAboutToBeDispatched)
		{
			foreach (IMiddleware middleWare in Middlewares)
				middleWare.BeforeDispatch(actionAboutToBeDispatched);
		}

		private void ExecuteMiddlewareAfterDispatch(object actionJustDispatched)
		{
			Middlewares.ForEach(x => x.AfterDispatch(actionJustDispatched));
		}

		private void ActivateStore()
		{
			if (HasActivatedStore)
				return;

			HasActivatedStore = true;
			InitializeMiddlewares();
			DequeueActions();
			InitializedCompletionSource.SetResult(true);
		}

		private void DequeueActions()
		{
			while (QueuedActions.Any())
			{
				// We want the next action but we won't dequeue it because we use
				// a non-empty queue as an indication that a Dispatch() loop is already in progress

				object nextActionToDequeue = QueuedActions.Peek();

				// Only process the action if no middleware vetos it
				if (Middlewares.All(x => x.MayDispatchAction(nextActionToDequeue)))
				{
					ExecuteMiddlewareBeforeDispatch(nextActionToDequeue);

					// Notify all features of this action
					foreach (var featureInstance in FeaturesByName.Values)
					{
						IFeatureReceiveDispatchNotificationFromStore(featureInstance, nextActionToDequeue);
					}

					ExecuteMiddlewareAfterDispatch(nextActionToDequeue);

					TriggerEffects(nextActionToDequeue);

					// handle Reactions

					var historyEntry = ActionHistory.LastOrDefault(x => x.Action == nextActionToDequeue);
					var root = historyEntry?.GetRoot() ?? null;

					if (historyEntry != null && root != null)
					{
						var reactionItemsRegisteredInRoot = root.ReactionItems;

						var reactionTypeKey = nextActionToDequeue.GetType();

						var reactionItem =
							reactionItemsRegisteredInRoot.SingleOrDefault(x => x.ActionType == reactionTypeKey);

						if (reactionItem != null)
						{
							Console.WriteLine($"--- invoke...{reactionItem.Action.GetType()}");
							// execute reaction
							reactionItem.Action.Invoke(nextActionToDequeue);
							root.Invoked = true;
						}
					}
					
					// TODO: better data structure than list?

					// smart clear history entries: 
					// - all configured items processed
					// - timeout

					ActionHistory.RemoveAll(x => x.DateTime < DateTime.UtcNow - TimeSpan.FromSeconds(10));
					ActionHistory.RemoveAll(x => x.Invoked);

					Console.WriteLine($"ActionHistory size is {ActionHistory.Count}");


					// check if ActionReactionDict has an entry
					//if (ActionReactionDict.TryGetValue(nextActionToDequeue, out IHasReaction baseAction))
					//{
					//	var actionKey = baseAction;

					//	// only use history?


					//	//// IHasReaction only => ActionTypeReactionItemDict
					//	////if (nextActionToDequeue is IHasReaction actionKey)
					//	//{
					//	//	Console.WriteLine("ReactionXyz here actionKey");
					//	//	//var guidKey = reactionGuidKey.ParentActionGuid;
					//	//	if (ActionTypeReactionItemDict.TryGetValue(actionKey,
					//	//		out Dictionary<Type, ReactionItem> typeReactionItemDict))
					//	//	{
					//	//		Console.WriteLine("found reaction actionKey");

					//	//		var actionHistoryEntry =
					//	//			ActionHistory.LastOrDefault(x => x.Action == nextActionToDequeue);

					//	//		var reactionTypeKey = nextActionToDequeue.GetType();

					//	//		//var reactionTypeKey = actionHistoryEntry?.RootAction.GetType() ?? nextActionToDequeue.GetType();
					//	//		Console.WriteLine($"found reaction actionKey (root type: {reactionTypeKey?.Name})");

					//	//		//if (actionHistoryEntry != null)
					//	//		//{
					//	//		//	Console.WriteLine($"--- invoke via history entry...{actionHistoryEntry.Action.GetType()}");\
					//	//		//	// execute reaction
					//	//		//	actionHistoryEntry.ActionToInvoke.Invoke(nextActionToDequeue);
					//	//		//}
					//	//		//else 
					//	//		if (typeReactionItemDict.TryGetValue(reactionTypeKey, out ReactionItem reactionItem))
					//	//		{
					//	//			Console.WriteLine($"--- invoke...{reactionItem.Action.GetType()}");
					//	//			// execute reaction
					//	//			reactionItem.Action.Invoke(nextActionToDequeue);

					//	//			//	// clean up dict
					//	//			//	var removeGuid = reactionItem.Guid;
					//	//			//	var removeKeys = TypeReactionItemDict.Where(x => x.Value.Guid == removeGuid).Select(x => x.Key).ToList();
					//	//			//	foreach (var removeKey in removeKeys)
					//	//			//	{
					//	//			//		TypeReactionItemDict.Remove(removeKey);
					//	//			//	}
					//	//		}

					//	//		// todo: clean with timeouts?

					//	//		// clean up dicts
					//	//		//ActionTypeReactionItemDict.Remove(actionKey);
					//	//		//ActionReactionDict.Remove(nextActionToDequeue);
					//	//	}

					//	//	Console.WriteLine($"ActionTypeReactionItemDict size is {ActionTypeReactionItemDict.Count}");
					//	//}
					//}

					//// ReactionBase => GuidTypeReactionItemDict
					//if (nextActionToDequeue is ReactionBase reactionGuidKey)
					//{
					//	Console.WriteLine("Reaction here");
					//	var guidKey = reactionGuidKey.ParentActionGuid;
					//	if (GuidTypeReactionItemDict.TryGetValue(guidKey,
					//		out Dictionary<Type, ReactionItem> typeReactionItemDict))
					//	{
					//		Console.WriteLine("found reaction guid");
					//		var reactionTypeKey = nextActionToDequeue.GetType();

					//		if (typeReactionItemDict.TryGetValue(reactionTypeKey, out ReactionItem reactionItem))
					//		{
					//			Console.WriteLine($"--- invoke...{reactionItem.Action.GetType()}");
					//			// execute reaction
					//			reactionItem.Action.Invoke(nextActionToDequeue);

					//			//	// clean up dict
					//			//	var removeGuid = reactionItem.Guid;
					//			//	var removeKeys = TypeReactionItemDict.Where(x => x.Value.Guid == removeGuid).Select(x => x.Key).ToList();
					//			//	foreach (var removeKey in removeKeys)
					//			//	{
					//			//		TypeReactionItemDict.Remove(removeKey);
					//			//	}
					//		}
					//		// clean up dict

					//		GuidTypeReactionItemDict.Remove(guidKey);
					//	}

					//	Console.WriteLine($"GuidTypeReactionItemDict size is {GuidTypeReactionItemDict.Count}");
					//}


					//if (TypeReactionItemDict.TryGetValue(reactionKey, out ReactionItem reactionItem))
					//{
					//	// Console.WriteLine($"--- invoke...{reactionItem.Action.GetType()}");
					//	// execute reaction
					//	reactionItem.Action.Invoke(nextActionToDequeue);

					//	// clean up dict
					//	var removeGuid = reactionItem.Guid;
					//	var removeKeys = TypeReactionItemDict.Where(x => x.Value.Guid == removeGuid).Select(x => x.Key).ToList();
					//	foreach (var removeKey in removeKeys)
					//	{
					//		TypeReactionItemDict.Remove(removeKey);
					//	}
					//}
				}

				// Now remove the processed action from the queue so we can move on to the next (if any)
				QueuedActions.Dequeue();
			}
		}
	}
}