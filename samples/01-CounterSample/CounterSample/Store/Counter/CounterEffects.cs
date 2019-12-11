using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blazor.Fluxor;
using CounterSample.Store.Counter.Increment;

namespace CounterSample.Store.Counter
{
    public class CounterEffects
    {
        private readonly IState<CounterState> _counterState;

        public CounterEffects(IState<CounterState> counterState)
        {
            _counterState = counterState;
        }

        [EffectMethod]
        public async Task HandleAsync(IncrementCounterAction action, IDispatcher dispatcher)
        {
            //if (_counterState.Value.ClickCount % 2 == 0)
            //{
            //    //var reaction = action.CreateIncrementCounterIsNowEvenResultAction();
            //    //reaction.IsEven   = true;

            //    //dispatcher.Dispatch(reaction);

            //    dispatcher.DispatchReaction(action, new IncrementCounterIsNowEvenResultAction()
            //    {
            //        IsEven = true
            //    });

            //    // alt, without guid?
            //    // TODO: implement this:
            //    // dispatcher.Dispatch(reaction, action);
            //    // OR:

            //    // dispatcher.DispatchReaction(reaction, action);
            //}
            //else
            {
                //var reaction = action.CreateResultReaction();
                //reaction.Count   = 123;
                //reaction.Message = $"from {DateTime.Now}";

                //dispatcher.Dispatch(reaction);
                Console.WriteLine($"EFFECT IncrementCounterAction");

                await Task.Delay(100);

                dispatcher.DispatchReaction(action, TimeSpan.FromSeconds(3),
                    new IncrementCounterResultAction()
                    {
                        Count   = 654,
                        Message = $"from {DateTime.Now}"
                    });

                

                // fast deep cloner : clone to?

                //dispatcher.Dispatch(new IncrementCounterResultAction()
                //{
                //    Count   = 123,
                //    Message = $"from {DateTime.Now}"
                //});
            }

            //return Task.CompletedTask;
        }

        [EffectMethod]
        public async Task HandleAsync(IncrementCounterResultAction action, IDispatcher dispatcher)
        {
            Console.WriteLine($"EFFECT IncrementCounterResultAction");

            // delay a bit for fun
            await Task.Delay(300);

            // BUG: if the following is enabled/dispatched,
            //      BUT there is no configuration in the original call from the blazor component
            //      (no (IncrementCounterIsNowEvenResultAction resultAction) => {}), the previous items dont get called correctly!

            // => check if registered, otherwise just dispatch normally!

            dispatcher.DispatchReaction(action, TimeSpan.FromSeconds(3),
                new IncrementCounterIsNowEvenResultAction()
                {
                    IsEven = _counterState.Value.ClickCount % 2 == 0
                });

            //// this works:
            //dispatcher.Dispatch(
            //    new IncrementCounterIsNowEvenResultAction()
            //    {
            //        IsEven = _counterState.Value.ClickCount % 2 == 0
            //    });

            //return Task.CompletedTask;
        }
    }
}