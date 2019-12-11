using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Blazor.Fluxor.Reactions;
using EnsureThat;
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

		private readonly List<ActionChainItem> ActionChains = new List<ActionChainItem>();

		private readonly Dictionary<object, ActionChainItem> ActionActionChainDict =
			new Dictionary<object, ActionChainItem>();

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

			Dispatch(new StoreInitializedAction(), TimeSpan.FromSeconds(3));
		}

		/// <see cref="IStore.AddFeature(IFeature)"/>
		public void AddFeature(IFeature feature)
		{
			if (feature == null)
				throw new ArgumentNullException(nameof(feature));

			FeaturesByName.Add(feature.GetName(), feature);
		}

		public void DispatchReaction(object baseAction, TimeSpan timeout, object reaction)
		{
			// note the SWITCH of baseAction/reaction parameter order:
			Dispatch<object, object, object>(reaction, baseAction, timeout, null, null, null);
		}

		public void Dispatch(object action)
		{
			Dispatch<object, object, object>(action, null, TimeSpan.FromSeconds(20), null, null, null);
		}

		public void Dispatch(object action, TimeSpan? timeout)
		{
			Dispatch<object, object, object>(action, null, timeout, null, null, null);
		}

		/// <see cref="IDispatcher.Dispatch"/>
		//public void Dispatch<T>(object action, Action<IResultAction<T>> resultAction)
		public void Dispatch<T>(object action, TimeSpan? timeout, Action<T> resultAction)
		{
			Dispatch<T, object, object>(action, null, timeout, resultAction, null, null);
		}

		public void Dispatch<T1, T2>(object action, TimeSpan? timeout, Action<T1> resultAction1,
			Action<T2> resultAction2)
		{
			Dispatch<T1, T2, object>(action, null, timeout, resultAction1, resultAction2, null);
		}

		public void Dispatch<T1, T2, T3>(object action, TimeSpan? timeout,
			Action<T1> resultAction1, Action<T2> resultAction2, Action<T3> resultAction3)
		{
			Dispatch<T1, T2, T3>(action, null, timeout, resultAction1, resultAction2, resultAction3);
		}

		public void Dispatch<T1, T2, T3>(object action,
			object baseAction, TimeSpan? timeout,
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
			//Console.WriteLine($"dispatch...wasAlreadyDispatching: {wasAlreadyDispatching}");

			QueuedActions.Enqueue(action);

			QueueReactions(action, baseAction, timeout, resultAction1, resultAction2, resultAction3);

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

			//var guid      = Guid.NewGuid();
			var timestamp = DateTime.Now;

			if (resultAction1 != null)
			{
				resultActions.Add(CreateReactionItem(resultAction1, timestamp));
			}

			if (resultAction2 != null)
			{
				resultActions.Add(CreateReactionItem(resultAction2, timestamp));
			}

			if (resultAction3 != null)
			{
				resultActions.Add(CreateReactionItem(resultAction3, timestamp));
			}

			return resultActions;
		}

		private ReactionItem CreateReactionItem<T>(Action<T> action, DateTime timestamp)
		{
			return new ReactionItem() {
				Action     = Convert(action),
				ActionType = typeof(T),
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
			try
			{
				while (QueuedActions.Any())
				{
					// We want the next action but we won't dequeue it because we use
					// a non-empty queue as an indication that a Dispatch() loop is already in progress

					object nextActionToDequeue = QueuedActions.Peek();
					//Console.WriteLine($"==>>> DequeuAction start loop for {nextActionToDequeue.GetType().Name}");

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

						TriggerReactions(nextActionToDequeue);
					}

					// Now remove the processed action from the queue so we can move on to the next (if any)
					QueuedActions.Dequeue();

					//Console.WriteLine($"==>>> DequeuAction end loop for {nextActionToDequeue.GetType().Name}");
				}

				//Console.WriteLine($"==>>> DequeuAction EXIT");
			}
			catch (Exception exception)
			{
				Console.WriteLine($"DequeuAction EXCEPTION: {exception}");
				throw;
			}
		}

		private void QueueReactions<T1, T2, T3>(object action, object baseAction, TimeSpan? timeout,
			Action<T1> resultAction1, Action<T2> resultAction2, Action<T3> resultAction3)
		{
			// prepare reaction items
			var reactionItems  = GetReactionItems(resultAction1, resultAction2, resultAction3);
			var expirationDate = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(30)); // default 30 seconds (?)

			if (baseAction == null)
			{
				//Console.WriteLine($"dispatch...only an action: {action.GetType().Name}");
				var rootChainItem = new ActionChainItem() {
					Action         = action,
					Parent         = null, // null is root
					ExpirationDate = expirationDate,
					ReactionItems  = reactionItems
				};
				ActionChains.Add(rootChainItem);
				ActionActionChainDict.Add(action, rootChainItem);
			}
			else
			{
				//Console.WriteLine($"dispatch...there is a baseAction: {baseAction.GetType().Name}");
				//var parent = ActionChains.LastOrDefault(x => x.Action == baseAction);
				var parentChainItem = ActionActionChainDict.TryGetValue(baseAction, out var actionChainItem) ? actionChainItem : null;

				//Console.WriteLine($"   dispatch...parent is: {parent?.Action.GetType().Name ?? "(null)"}");

				// validate user intent
				var rootReactionItems = parentChainItem?.GetRoot().ReactionItems;
				if (parentChainItem == null || !rootReactionItems.Any() ||
				    rootReactionItems.All(x => x.ActionType != action.GetType()))
				{
					// output warning, verify library user intent
					Console.WriteLine(
						$"WARNING: There was no pre-registered reaction-chain for action => reaction '{baseAction.GetType().Name} => {action.GetType().Name}' (via DispatchReaction?)");
				}

				var subChainItem = new ActionChainItem() {
					Action         = action,
					Parent         = parentChainItem, // might still be null
					ExpirationDate = expirationDate,
					// ReactionItems = xxx // => do NOT set! only store/access in root actionChainItem!
				};
				// add current item
				ActionChains.Add(subChainItem);
				ActionActionChainDict.Add(action, subChainItem);
			}
		}

		private void TriggerReactions(object nextActionToDequeue)
		{
			// handle Reactions
			var actionChainItem = ActionChains.LastOrDefault(x => x.Action == nextActionToDequeue);
			var root            = actionChainItem?.GetRoot() ?? null;

			if (actionChainItem != null && root != null)
			{
				var reactionItemsRegisteredInRoot = root.ReactionItems;

				if (reactionItemsRegisteredInRoot.Any())
				{
					// Console.WriteLine($"*** reaction has entries for {nextActionToDequeue.GetType().Name}");
					var reactionTypeKey = nextActionToDequeue.GetType();

					var reactionItem =
						reactionItemsRegisteredInRoot.SingleOrDefault(x => x.ActionType == reactionTypeKey);

					if (reactionItem != null && !reactionItem.Invoked)
					{
						// execute reaction
						Console.WriteLine($"--- invoke...{reactionItem.ActionType.Name}");
						reactionItem.Action.Invoke(nextActionToDequeue);
						reactionItem.Invoked = true;
					}

					// ALL registered reactions invoked (in root?)
					if (reactionItemsRegisteredInRoot.TrueForAll(x => x.Invoked))
					{
						// remove root and all children
						var ancestors = actionChainItem.GetAncestors();
						ActionChains.RemoveAll(x => ancestors.Contains(x));
						
						foreach (var chainItem in ancestors)
						{
							Console.WriteLine($"ActionActionChainDict remove INVOKED item: {chainItem.Action.GetType().Name}");
							ActionActionChainDict.Remove(chainItem.Action);
						}
					}
				}
			}  

			var expiredActions = ActionChains.Where(x => x.ExpirationDate < DateTime.UtcNow).ToList();

			ActionChains.RemoveAll(x => x.ExpirationDate < DateTime.UtcNow);

			// process dict
			foreach (var expiredAction in expiredActions)
			{
				Console.WriteLine($"ActionActionChainDict remove EXPIRED item: {expiredAction.Action.GetType().Name}");
				ActionActionChainDict.Remove(expiredAction.Action);
			}
			Console.WriteLine($"ActionChains size: {ActionChains.Count}");
			Console.WriteLine($"ActionActionChainDict size: {ActionActionChainDict.Count}");
		}
	}
}