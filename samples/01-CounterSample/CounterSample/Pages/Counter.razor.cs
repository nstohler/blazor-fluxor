using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blazor.Fluxor;
using Blazor.Fluxor.Components;
using CounterSample.Store.Counter;
using CounterSample.Store.Counter.Increment;
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
            Console.WriteLine($"start action!");

            //// default action dispatch
            //Dispatcher.Dispatch(new IncrementCounterAction());

            //// action with 2 reactions 
            //Dispatcher.Dispatch(new IncrementCounterAction(), TimeSpan.FromSeconds(10),
            //    (IncrementCounterResultAction resultAction) =>
            //    {
            //        Console.WriteLine($"IncrementCounterResultAction received!");
            //        Console.WriteLine($"==> IncrementCounterResultAction is {resultAction.Message}");
            //    }
            //    // multiple registrations ? cleanup how ? timeouts ?
            //    ,
            //    (IncrementCounterIsNowEvenResultAction resultAction) =>
            //    {
            //        Console.WriteLine($"IncrementCounterIsNowEvenResultAction received!");
            //        Console.WriteLine($"==> IncrementCounterIsNowEvenResultAction is even: {resultAction.IsEven}");
            //    }
            //);

            // BUG: the following is buggy, if the effect for IncrementCounterResultAction dispatches another reaction for IncrementCounterIsNowEvenResultAction which is not enabled/registered here!!! => add checks/warnings
            // action with 2 reactions 
            Dispatcher.Dispatch(new IncrementCounterAction()
                //, TimeSpan.FromSeconds(10),
                //(IncrementCounterResultAction resultAction) =>
                //{
                //    Console.WriteLine($"RESULT: IncrementCounterResultAction received!");
                //    Console.WriteLine($"==> IncrementCounterResultAction is {resultAction.Message}");
                //}
                //// multiple registrations ? cleanup how ? timeouts ?
                //,
                //(IncrementCounterIsNowEvenResultAction resultAction) =>
                //{
                //    Console.WriteLine($"IS_NOW_EVEN: IncrementCounterIsNowEvenResultAction received!");
                //    Console.WriteLine($"==> IncrementCounterIsNowEvenResultAction is even: {resultAction.IsEven}");
                //}
            );

            // TODO: bug if only IncrementCounterResultAction active
            // => extend: show warning in logs/console for dispatched reactions that are unregistered
        }

        protected void IncrementCountNormal()
        {
            Dispatcher.Dispatch(new IncrementCounterAction());
        }
    }
}