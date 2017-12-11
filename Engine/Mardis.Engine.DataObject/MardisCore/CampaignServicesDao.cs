using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mardis.Engine.DataAccess;
using Mardis.Engine.DataAccess.MardisCore;
using Mardis.Engine.Framework.Resources;
using Microsoft.EntityFrameworkCore;

namespace Mardis.Engine.DataObject.MardisCore
{
    public class CampaignServicesDao : ADao
    {
        public CampaignServicesDao(MardisContext mardisContext) : base(mardisContext)
        {
        }

        public List<CampaignServices> GetCampaignServicesByCampaign(Guid idCampaign, Guid idAccount)
        {
            return Context.CampaignsServices
                .Include(cs => cs.Service)
                .Where(cs => cs.IdCampaign == idCampaign &&
                              cs.StatusRegister == CStatusRegister.Active &&
                              cs.IdAccount == idAccount)
                .ToList();
        }

        public List<CampaignServices> GetCampaignServicesByService(Guid idService, Guid idAccount)
        {
            return Context.CampaignsServices
                .Where(cs => cs.IdService == idService &&
                             cs.StatusRegister == CStatusRegister.Active &&
                             cs.IdAccount == idAccount)
                .ToList();
        }

        public async Task<IQueryable<CampaignServices>> GetCompleteCampaignServices(Guid idCampaign, Guid idAccount)
        {
            return await Task.FromResult(Context.CampaignsServices
                .Include(cs => cs.Service.ServiceDetails)
                    .ThenInclude(sd => sd.Questions)
                        .ThenInclude(q => q.TypePoll)
                .Include(cs => cs.Service.ServiceDetails)
                    .ThenInclude(sd => sd.Questions)
                        .ThenInclude(q => q.QuestionDetails)
                .Include(cs => cs.Service.ServiceDetails)
                    .ThenInclude(sd => sd.Sections)
                        .ThenInclude(s => s.Questions)
                            .ThenInclude(q => q.QuestionDetails)
                .Include(cs => cs.Service.ServiceDetails)
                    .ThenInclude(sd => sd.Sections)
                        .ThenInclude(s => s.Questions)
                            .ThenInclude(q => q.TypePoll)
                .Where(cs => cs.IdCampaign == idCampaign &&
                             cs.StatusRegister == CStatusRegister.Active &&
                             cs.IdAccount == idAccount));
        }
    }
}
