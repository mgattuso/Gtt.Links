using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gtt.CodeWorks;
using Gtt.CodeWorks.StateMachines;
using Stateless;

namespace Gtt.Links.Core.V1
{
    public class ShortLinkService : BaseStatefulServiceInstance<ShortLinkRequest, ShortLinkResponse, ShortLinkService.State, ShortLinkService.Trigger,
        ShortLinkService.Data>
    {
        public enum State
        {
            Pending,
            Active,
            Inactive
        }

        public enum Trigger
        {
            Create,
            Delete
        }

        public class Data : BaseStateDataModel<State>
        {
            public string OwnerEmailAddress { get; set; }
            public string OriginalUrl { get; set; }
        }

        public ShortLinkService(CoreDependencies coreDependencies, StatefulDependencies statefulDependencies) : base(coreDependencies, statefulDependencies)
        {
        }

        protected override IDictionary<int, string> DefineErrorCodes()
        {
            return NoErrorCodes();
        }

        protected override (Trigger Trigger, Func<ShortLinkRequest, string> Func)? DeriveIdentifier()
        {
            Random r = new Random(Guid.NewGuid().GetHashCode());
            char[] letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToArray();

            string code = "";
            for (int i = 0; i < 4; i++)
            {
                var idx = r.Next(0, letters.Length);
                code = code + letters[idx];
            }

            return (Trigger.Create, req => code);
        }

        protected override void Rules(StateMachine<State, Trigger> machine)
        {
            machine.Configure(State.Pending)
                .Permit(Trigger.Create, State.Active);

            machine.Configure(State.Active)
                .OnEntryFromAsync(Trigger.Create, OnCreation, "Create Link");

            machine.Configure(State.Active)
                .Permit(Trigger.Delete, State.Inactive);
        }

        private Task OnCreation(StateMachine<State, Trigger>.Transition transition)
        {
            var req = As<ShortLinkRequest.CreateData>(transition);
            CurrentData.OriginalUrl = req.Url;
            CurrentData.OwnerEmailAddress = req.EmailAddress;
            return Task.CompletedTask;
        }
    }

    public class ShortLinkRequest : BaseStatefulRequest<ShortLinkService.Trigger>
    {
        public CreateData Create { get; set; }

        public class CreateData
        {
            public string Url { get; set; }
            public string EmailAddress { get; set; }
        }
    }


    public class ShortLinkResponse : BaseStatefulResponse<ShortLinkService.State, ShortLinkService.Trigger, ShortLinkService.Data>
    {

    }
}
