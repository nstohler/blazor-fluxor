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
        [EffectMethod]
        public Task HandleAsync(IncrementCounterAction action, IDispatcher dispatcher)
        {
            dispatcher.Dispatch(new IncrementCounterResultAction()
            {
                Count = 123,
                Message = $"from {DateTime.Now}"
            });

            return Task.CompletedTask;
        }
    }
}
