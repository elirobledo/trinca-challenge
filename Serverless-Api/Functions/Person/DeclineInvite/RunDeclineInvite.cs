using Domain;
using Eveneum;
using CrossCutting;
using Domain.Events;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using static Domain.ServiceCollectionExtensions;

namespace Serverless_Api
{
    public partial class RunDeclineInvite
    {
        private readonly Person _user;
        private readonly IPersonRepository _repository;

        public RunDeclineInvite(Person user, IPersonRepository repository)
        {
            _user = user;
            _repository = repository;
        }

        [Function(nameof(RunDeclineInvite))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "put", Route = "person/invites/{inviteId}/decline")] HttpRequestData req, string inviteId)
        {
            var person = await _repository.GetAsync(_user.Id);

            if (person == null)
                return req.CreateResponse(System.Net.HttpStatusCode.NoContent);

            if(person.Invites.Where(i => i.Status == InviteStatus.Accepted).Count() == 0) {
                person.Apply(new InviteWasDeclined { InviteId = inviteId, PersonId = person.Id });
                
            }
            else
            {
                person.Apply(new InviteWasRecused { InviteId = inviteId, PersonId = person.Id });
            }
            

            await _repository.SaveAsync(person);
            //Implementar impacto da recusa do convite no churrasco caso ele já tivesse sido aceito antes

            return await req.CreateResponse(System.Net.HttpStatusCode.OK, person.TakeSnapshot());
        }
    }
}
