using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blazor.Fluxor;

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
        public Task HandleAsync(IncrementCounterAction action, IDispatcher dispatcher)
        {
            if (_counterState.Value.ClickCount % 2 == 0)
            {
                dispatcher.Dispatch(new IncrementCounterIsNowEvenResultAction()
                {
                    IsEven = true
                });
            }
            else
            {
                dispatcher.Dispatch(new IncrementCounterResultAction()
                {
                    Count   = 123,
                    Message = $"from {DateTime.Now}"
                });
            }
            return Task.CompletedTask;
        }
    }
}