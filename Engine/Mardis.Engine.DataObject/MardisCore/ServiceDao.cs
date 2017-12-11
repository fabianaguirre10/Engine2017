using System;
using System.Collections.Generic;
using System.Linq;
using Mardis.Engine.DataAccess;
using Mardis.Engine.DataAccess.MardisCore;
using Mardis.Engine.Framework.Resources;
using Microsoft.EntityFrameworkCore;

namespace Mardis.Engine.DataObject.MardisCore
{
    public class ServiceDao : ADao
    {
        public ServiceDao(MardisContext mardisContext)
               : base(mardisContext)
        {

        }

        public List<Service> GetService(Guid idTypeService, Guid idCustomer, Guid idAccount)
        {
            var itemsReturn = Context.Services
                                     .Join(Context.TypeServices,
                                        tb => tb.IdTypeService,
                                        ts => ts.Id,
                                        (tb, ts) => new { tb, ts })
                                     .Where(tb => tb.tb.IdCustomer == idCustomer &&
                                                  tb.tb.IdTypeService == idTypeService &&
                                                  tb.tb.IdAccount == idAccount &&
                                                  tb.ts.StatusRegister == CStatusRegister.Active &&
                                                  tb.tb.StatusRegister == CStatusRegister.Active)
                                     .Select(tb => tb.tb)
                                     .ToList();

            return itemsReturn;
        }

        public Service GetOne(Guid id, Guid idAccount)
        {
            var itemReturn = Context.Services
                .Include(s => s.ServiceDetails)
                .AsNoTracking()
                            .FirstOrDefault(tb => tb.Id == id &&
                                        tb.IdAccount == idAccount &&
                                        tb.StatusRegister == CStatusRegister.Active);

            return itemReturn;
        }

        public Service GetOneTraking(Guid id, Guid idAccount)
        {
            var itemReturn = Context.Services
                            .Include(s => s.ServiceDetails)
                            .FirstOrDefault(tb => tb.Id == id &&
                                        tb.IdAccount == idAccount &&
                                        tb.StatusRegister == CStatusRegister.Active);

            return itemReturn;
        }

        public List<Service> GetServicesByCustomerId(Guid idAccount, Guid idCustomer)
        {
            return Context.Services
                .Where(srv => srv.IdCustomer == idCustomer
                        && srv.IdAccount == idAccount
                        && srv.StatusRegister == CStatusRegister.Active)
                .ToList();
        }

        public List<Service> GetServicesByChannelId(Guid idAccount, Guid idChannel)
        {
            return Context.Services
                .Where(srv => srv.IdChannel == idChannel
                        && srv.IdAccount == idAccount
                        && srv.StatusRegister == CStatusRegister.Active)
                .ToList();
        }
        public List<Service> GetServicesByAccount(Guid idAccount)
        {
            return Context.Services
                .Where(srv => srv.IdAccount == idAccount
                        && srv.StatusRegister == CStatusRegister.Active)
                .ToList();
        }

        /// <summary>
        /// Obtiene el Servicio completo sin Tracking
        /// </summary>
        /// <param name="idService">Id del Servicio a ser consultado</param>
        /// <param name="idAccount">Id de la Cuenta general de la sesión actual</param>
        /// <returns>Objeto completamente mapeado sin Tracking</returns>
        public Service GetService(Guid idService, Guid idAccount)
        {
            var service = Context.Services
                .Include(s => s.ServiceDetails)
                   .ThenInclude(sd => sd.Questions)
                        .ThenInclude(q => q.QuestionDetails)
                .Include(s => s.ServiceDetails)
                    .ThenInclude(sd => sd.Sections)
                        .ThenInclude(sc => sc.Questions)
                            .ThenInclude(q => q.QuestionDetails)
                .AsNoTracking()
                .FirstOrDefault(s => s.Id == idService && s.IdAccount == idAccount);

            return service;
        }
    }
}
