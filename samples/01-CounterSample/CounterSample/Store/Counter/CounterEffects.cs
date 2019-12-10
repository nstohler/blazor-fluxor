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

                //await Task.Delay(500);
                await Task.Delay(10);

                dispatcher.DispatchReaction(action, new IncrementCounterResultAction()
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
        public Task HandleAsync(IncrementCounterResultAction action, IDispatcher dispatcher)
        {
            Console.WriteLine($"EFFECT IncrementCounterResultAction");
            dispatcher.DispatchReaction(action, new IncrementCounterIsNowEvenResultAction()
            {
                IsEven = _counterState.Value.ClickCount % 2 == 0
            });

            return Task.CompletedTask;
        }
    }
}