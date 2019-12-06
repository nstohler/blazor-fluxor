using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
		//private readonly Queue<(object action, object reaction)> QueuedActions = new Queue<(object action, object reaction)>();

		//private readonly Dictionary<Type, List<Action<object>>> Reactions = new Dictionary<Type, List<Action<object>>>();
		private readonly Dictionary<Type, ReactionItem> TypeReactionItemDict = new Dictionary<Type, ReactionItem>();
		//private readonly Dictionary<string, ReactionItem> GuidReactionItemDict = new Dictionary<string, ReactionItem>();

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

		public void Dispatch(object action)
		{
			Dispatch<object, object, object>(action, null, null, null);
		}

		/// <see cref="IDispatcher.Dispatch"/>
		//public void Dispatch<T>(object action, Action<IResultAction<T>> resultAction)
		public void Dispatch<T>(object action, Action<T> resultAction)
		{
			Dispatch<T, object, object>(action, resultAction, null, null);
		}

		public void Dispatch<T1, T2>(object action, Action<T1> resultAction1, Action<T2> resultAction2)
		{
			Dispatch<T1, T2, object>(action, resultAction1, resultAction2, null);
		}

		public void Dispatch<T1, T2, T3>(object action, Action<T1> resultAction1, Action<T2> resultAction2,
			Action<T3> resultAction3)
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

			var reactionItems = GetReactionItems(resultAction1, resultAction2, resultAction3);

			// store reactionItems
			foreach (var reactionItem in reactionItems)
			{
				var key = reactionItem.ActionType;
				Console.WriteLine($"--adding to dict: {key.FullName}");
				if (!TypeReactionItemDict.ContainsKey(key))
				{
					//TypeReactionItemDict.Add(key, new List<ReactionItem>());
					TypeReactionItemDict.Add(key, reactionItem);
				}
				//TypeReactionItemDict[key].Add(reactionItem);
			}
			
			//// store resultAction
			//if (resultAction != null)
			//{
			//	//var key = resultAction.GetType();
			//	var key = typeof(T);

			//	if (!Reactions.ContainsKey(key))
			//	{
			//		//Reactions.Add(key, new List<object>());
			//		Reactions.Add(key, new List<Action<object>>());
			//	}

			//	//Reactions[key].Add(resultAction);
			//	Reactions[key].Add(Convert(resultAction));
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
				
				//var item                = QueuedActions.Peek();
				//var nextActionToDequeue = item.action;

				// Only process the action if no middleware vetos it
				if (Middlewares.All(x => x.MayDispatchAction(nextActionToDequeue)))
				{
					ExecuteMiddlewareBeforeDispatch(nextActionToDequeue);

					// Notify all features of this action
					foreach (var featureInstance in FeaturesByName.Values)
						IFeatureReceiveDispatchNotificationFromStore(featureInstance, nextActionToDequeue);

					ExecuteMiddlewareAfterDispatch(nextActionToDequeue);

					TriggerEffects(nextActionToDequeue);

					Console.WriteLine($"DequeueActionsX: {nextActionToDequeue.GetType().Name}");
					Console.WriteLine($"DequeueActionsX: {TypeReactionItemDict.Count}");

					foreach (var reactionItemX in TypeReactionItemDict)
					{
						Console.WriteLine($" => registered reaction type: [{reactionItemX.Key.Name}]");
					}

					// check if this action (nextActionToDequeue) has an entry in Reactions
					//if (Reactions.ContainsKey(nextActionToDequeue.GetType()))
					var reactionKey = nextActionToDequeue.GetType();

					//if (TypeReactionItemDict.TryGetValue(reactionKey, out List<ReactionItem> reactionItems))
					if (TypeReactionItemDict.TryGetValue(reactionKey, out ReactionItem reactionItem))
					{
						//Console.WriteLine($"- found entry: {reactionKey} | {reactionItems.Count}");

						//foreach (var reactionItem in reactionItems)
						{
							Console.WriteLine($"-- item: {reactionItem.Action.GetType()}");

							//if (reactionItem is Action<IResultAction<object>> r)
							if (reactionItem.Action is Action<object> r)	// not really required, should be always true!
							{
								Console.WriteLine($"--- invoke...{reactionItem.Action.GetType()}");
								r.Invoke(nextActionToDequeue);
							}
						}

						var removeGuid = reactionItem.Guid;

						//TypeReactionItemDict = TypeReactionItemDict.Where(x => x.Value.Guid != removeGuid)
						//	.ToDictionary(x => x);

						var removeKeys = TypeReactionItemDict.Where(x => x.Value.Guid == removeGuid).Select(x => x.Key).ToList();
						foreach (var removeKey in removeKeys)
						{
							TypeReactionItemDict.Remove(removeKey);
						}

						//TypeReactionItemDict.Remove(reactionKey);

						//remove all items with same guid
						
					}

					// test: invoke reaction if present
					//if (item.reaction != null)
					//{
					//	if (item.reaction is Action<IResultAction<object>> r)
					//	{
					//		r.Invoke(null);
					//	}
					//}
				}

				// Now remove the processed action from the queue so we can move on to the next (if any)
				QueuedActions.Dequeue();
			}
		}
	}
}