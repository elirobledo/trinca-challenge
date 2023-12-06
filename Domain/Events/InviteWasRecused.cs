using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Events
{
    public class InviteWasRecused : IEvent
    {
        public string InviteId { get; set; }
        public string PersonId { get; set; }
    }
}
