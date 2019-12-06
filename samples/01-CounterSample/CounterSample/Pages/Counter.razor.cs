using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blazor.Fluxor;
using Blazor.Fluxor.Components;
using CounterSample.Store.Counter;
using Microsoft.AspNetCore.Components;

namespace CounterSample.Pages
{
    public class CounterBase : FluxorComponent
    {
        [Inject] protected IDispatcher          Dispatcher   { get; set; }
        [Inject] protected IState<CounterState> CounterState { get; set; }

        protected void IncrementCount()
        {
            //Dispatcher.Dispatch(new IncrementCounterAction(),
            //    (IncrementCounterResultAction resultAction) =>
            //    {
            //        Console.WriteLine($"Reaction received!");

            //        if (resultAction is IncrementCounterResultAction result)
            //        {
            //            Console.WriteLine($"Reaction is {result.Message}");
            //        }
            //    });

            // TODO:

            Dispatcher.Dispatch(new IncrementCounterAction(),
                (IncrementCounterResultAction resultAction) =>
                {
                    Console.WriteLine($"IncrementCounterResultAction received!");
                    Console.WriteLine($"==> IncrementCounterResultAction is {resultAction.Message}");
                },
                // multiple registrations ? cleanup how ? timeouts ?
                (IncrementCounterIsNowEvenResultAction resultAction) =>
                {
                    Console.WriteLine($"IncrementCounterIsNowEvenResultAction received!");
                    Console.WriteLine($"==> IncrementCounterIsNowEvenResultAction is even: {resultAction.IsEven}");
                }
            );
        }

        protected void IncrementCountNormal()
        {
            Dispatcher.Dispatch(new IncrementCounterAction());
        }
    }
}