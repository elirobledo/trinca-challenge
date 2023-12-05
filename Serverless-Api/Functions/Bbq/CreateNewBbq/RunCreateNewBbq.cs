using Eveneum;
using System.Net;
using CrossCutting;
using Domain.Events;
using Domain.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;

namespace Serverless_Api
{
    public partial class RunCreateNewBbq
    {
        private readonly Person _user;
        private readonly SnapshotStore _snapshots;
        private readonly IEventStore<Bbq> _bbqsStore;
        private readonly IEventStore<Person> _peopleStore;
        public RunCreateNewBbq(IEventStore<Bbq> eventStore, IEventStore<Person> peopleStore, SnapshotStore snapshots, Person user)
        {
            _user = user;
            _snapshots = snapshots;
            _bbqsStore = eventStore;
            _peopleStore = peopleStore;
        }

        [Function(nameof(RunCreateNewBbq))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "churras")] HttpRequestData req)
        {
            var input = await req.Body<NewBbqRequest>();

            if (input == null)
            {
                return await req.CreateResponse(HttpStatusCode.BadRequest, "input is required.");
            }

            var churras = new Bbq();
            churras.Apply(new ThereIsSomeoneElseInTheMood(Guid.NewGuid(), input.Date, input.Reason, input.IsTrincasPaying));

            await _bbqsStore.WriteToStream(churras.Id, churras.Changes.Select(evento => new EventData(churras.Id, evento, new { CreatedBy = _user.Id }, churras.Version, DateTime.Now.ToString())).ToArray(), expectedVersion: churras.Version == 0 ? null : churras.Version);

            var churrasSnapshot = churras.TakeSnapshot();

            var Lookups = await _snapshots.AsQueryable<Lookups>("Lookups").SingleOrDefaultAsync();

            foreach (var personId in Lookups.ModeratorIds)
            {
                var header = await _peopleStore.ReadHeader(personId);

                await _peopleStore.WriteToStream(personId, new[] { new EventData(personId, new PersonHasBeenInvitedToBbq(churras.Id, churras.Date, churras.Reason) , new { CreatedBy = _user.Id }, header.StreamHeader.Version, DateTime.Now.ToString()) }, expectedVersion: header.StreamHeader.Version == 0 ? null : header.StreamHeader.Version);
            }

            return await req.CreateResponse(HttpStatusCode.Created, churrasSnapshot);
        }
    }
}
