using Blazor.Fluxor.Reactions;

namespace CounterSample.Store.Counter.Increment
{
    public class IncrementCounterAction : IHasReaction
    {
        // - store action GUID here, 
        // - provide option to create Reaction action only through this action?

        //public IncrementCounterResultAction CreateResultReaction()
        //{
        //    var reaction = new IncrementCounterResultAction()
        //    {
        //        ParentActionGuid = this.ActionGuid
        //    };
        //    return reaction;
        //}

        //public IncrementCounterIsNowEvenResultAction CreateIncrementCounterIsNowEvenResultAction()
        //{
        //    var reaction = new IncrementCounterIsNowEvenResultAction()
        //    {
        //        ParentActionGuid = this.ActionGuid
        //    };
        //    return reaction;
        //}
    }

    //public class IncrementCounterAction : ActionWithReactionBase, IHasReaction
    //   {
    //       // - store action GUID here, 
    //       // - provide option to create Reaction action only through this action?

    //       public IncrementCounterResultAction CreateResultReaction()
    //       {
    //           var reaction = new IncrementCounterResultAction()
    //           {
    //               ParentActionGuid = this.ActionGuid
    //           };
    //           return reaction;
    //       }

    //       public IncrementCounterIsNowEvenResultAction CreateIncrementCounterIsNowEvenResultAction()
    //       {
    //           var reaction = new IncrementCounterIsNowEvenResultAction()
    //           {
    //               ParentActionGuid = this.ActionGuid
    //           };
    //           return reaction;
    //       }
    //   }
}
