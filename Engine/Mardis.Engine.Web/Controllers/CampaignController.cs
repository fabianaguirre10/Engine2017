#region Using

using System;
using System.Collections.Generic;
using System.Linq;
using Mardis.Engine.Business;
using Mardis.Engine.Business.MardisCore;
using Mardis.Engine.Business.MardisSecurity;
using Mardis.Engine.DataAccess;
using Mardis.Engine.DataAccess.MardisCore;
using Mardis.Engine.Framework;
using Mardis.Engine.Framework.Resources.PagesConstants;
using Mardis.Engine.Web.Libraries.Security;
using Mardis.Engine.Web.Libraries.Services;
using Mardis.Engine.Web.Model;
using Mardis.Engine.Web.ViewModel;
using Mardis.Engine.Web.ViewModel.CampaignViewModels;
using Mardis.Engine.Web.ViewModel.Filter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using Microsoft.Extensions.Caching.Distributed;

#endregion

namespace Mardis.Engine.Web.Controllers
{
    [Authorize]
    public class CampaignController : AController<CampaignController>
    {
        #region Variables & Constructores
        private readonly CampaignBusiness _campaignBusiness;
        private readonly TaskCampaignBusiness _taskCampaignBusiness;
        private readonly CommonBusiness _commonBusiness;
        private readonly CustomerBusiness _customerBusiness;
        private readonly StatusCampaignBusiness _statusCampaignBusiness;
        private readonly UserBusiness _userBusiness;
        private readonly ChannelBusiness _channelBusiness;
        private readonly ServiceBusiness _serviceBusiness;
        public static IDataProtector Protector;
        private readonly StatusTaskBusiness _statusTaskBusiness;
        private readonly TaskNotImplementedReasonBusiness _taskNotImplementedReasonBusiness;
        public Guid _userId;
        public Guid _Profile;
        public Guid _typeuser;
        public readonly ProfileBusiness _profileBusiness;

        public CampaignController(
                                    UserManager<ApplicationUser> userManager,
                                    IHttpContextAccessor httpContextAccessor,
                                    MardisContext mardisContext,
                                    ILogger<CampaignController> logger,
                                    ILogger<ServicesFilterController> loggeFilter,
                                    IDataProtectionProvider protectorProvider,
                                    IMemoryCache memoryCache,
                                    RedisCache distributedCache) :
            base(userManager, httpContextAccessor, mardisContext, logger)
        {
            Protector = protectorProvider.CreateProtector(GetType().FullName);
            _campaignBusiness = new CampaignBusiness(mardisContext);
            TableName = CCampaign.TableName;
            ControllerName = CCampaign.Controller;
            _taskCampaignBusiness = new TaskCampaignBusiness(mardisContext,distributedCache);
            _commonBusiness = new CommonBusiness(mardisContext);
            _customerBusiness = new CustomerBusiness(mardisContext);
            _statusCampaignBusiness = new StatusCampaignBusiness(mardisContext, memoryCache);
            _userBusiness = new UserBusiness(mardisContext);
            _channelBusiness = new ChannelBusiness(mardisContext);
            _serviceBusiness = new ServiceBusiness(mardisContext);
            _statusTaskBusiness = new StatusTaskBusiness(mardisContext, distributedCache);
            _taskNotImplementedReasonBusiness = new TaskNotImplementedReasonBusiness(mardisContext);
            _profileBusiness = new ProfileBusiness(mardisContext);
            if (ApplicationUserCurrent.UserId != null)
            {

                _userId = new Guid(ApplicationUserCurrent.UserId);
                _Profile = ApplicationUserCurrent.ProfileId;
                _typeuser = _profileBusiness.GetById(_Profile).IdTypeUser;
            }
        }
        #endregion

        [HttpGet]
        public string GetCampaignById(string idCampaign)
        {
            try
            {
                var itemReturn = new Campaign();

                if (!string.IsNullOrEmpty(idCampaign))
                {
                    itemReturn = _campaignBusiness.GetCampaignById(new Guid(idCampaign), ApplicationUserCurrent.AccountId);
                }

                return JSonConvertUtil.Convert(itemReturn);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return null;
            }
        }

        [HttpGet]
        public string GetSimpleCampaignById(string idCampaign)
        {
            try
            {
                var itemReturn = new Campaign();

                if (!string.IsNullOrEmpty(idCampaign))
                {
                    itemReturn = _campaignBusiness.GetSimpleCampaignById(new Guid(idCampaign), ApplicationUserCurrent.AccountId);
                }

                return JSonConvertUtil.Convert(itemReturn);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return null;
            }
        }

        [HttpGet]
        public string GetCampaignByName(string nameCampaign)
        {
            try
            {
                Campaign itemReturn = null;

                if (!string.IsNullOrEmpty(nameCampaign))
                {
                    itemReturn = _campaignBusiness.GetCampaignByName(nameCampaign, ApplicationUserCurrent.AccountId);
                }

                return JSonConvertUtil.Convert(itemReturn);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return null;
            }
        }

        [HttpGet]
        public IActionResult Register(string idCampaign)
        {
            try
            {
                var id = Guid.Empty;
                if (!string.IsNullOrEmpty(idCampaign))
                {
                    id = Guid.Parse(Protector.Unprotect(idCampaign));
                }
                var model = _campaignBusiness.GetCampaign(id, ApplicationUserCurrent.AccountId);

                GetDropDownListData(model);

                return View(model);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }
        }

        [HttpPost]
        public IActionResult Register(CampaignRegisterViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    GetDropDownListData(model);
                    return View(model);
                }
                _campaignBusiness.Save(model, ApplicationUserCurrent.AccountId);
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }
        }

        private void GetDropDownListData(CampaignRegisterViewModel model)
        {
            var myWatch = new Stopwatch();
            ViewBag.CustomerList =
                _customerBusiness.GetCustomersByAccount(ApplicationUserCurrent.AccountId)
                    .Select(s => new SelectListItem() { Text = s.Name, Value = s.Id.ToString() })
                    .ToList();

            myWatch.Start();
            ViewBag.StatusList =
                _statusCampaignBusiness.GetStatusCampaigns()
                    .Select(s => new SelectListItem() { Text = s.Name, Value = s.Id.ToString() })
                    .ToList();
            myWatch.Stop();

            Debugger.Log(0, "Drop", $"ms: {myWatch.ElapsedMilliseconds}");

            ViewBag.SupervisorList =
                _userBusiness.GetUserListByType(CTypePerson.PersonSupervisor, ApplicationUserCurrent.AccountId)
                    .Select(s => new SelectListItem() { Text = s.Profile.Name, Value = s.Id.ToString() })
                    .ToList();

            ViewBag.ChannelList =
                _channelBusiness.GetChanelListByCustomer(model.IdCustomer, ApplicationUserCurrent.AccountId);

            ViewBag.Services =
                _serviceBusiness.GetServicesByChannelId(ApplicationUserCurrent.AccountId, model.IdChannel)
                    .Select(c => new SelectListItem() { Text = c.Name, Value = c.Id.ToString() })
                    .ToList();
        }

        #region Guardar

        [HttpPost]
        public string SaveCampaign(Campaign campaign, string inputServices)
        {
            try
            {
                var idAccount = ApplicationUserCurrent.AccountId;
                var itemServices = JsonConvert.DeserializeObject<List<ListCampaignServicesViewModel>>(inputServices);

                campaign = _campaignBusiness.SaveCampaign(campaign, itemServices, idAccount);

                return JSonConvertUtil.Convert(campaign);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return null;
            }
        }

        #endregion

        [HttpPost]
        public override bool Delete(string input)
        {
            var itemIds = JsonConvert.DeserializeObject<string[]>(input);

            var campaignIds = itemIds.Select(i => Protector.Unprotect(i)).ToList();

            var idTasks = "";

            foreach (var tasks in itemIds.Select(item => _taskCampaignBusiness.GetAlltasksByCampaignId(new Guid(Protector.Unprotect(item)), ApplicationUserCurrent.AccountId)))
            {
                foreach (var tsk in tasks)
                {
                    if ((idTasks.IndexOf(',') < 0) && (!(string.IsNullOrEmpty(idTasks))) ||
                        (idTasks.IndexOf(',') >= 0) && (!(string.IsNullOrEmpty(idTasks))))
                    {
                        idTasks += ",";
                    }
                    idTasks += "\"" + tsk.Id + "\"";
                }
                idTasks = "[" + idTasks + "]";

                var idsTask = JsonConvert.DeserializeObject<string[]>(idTasks);

                if (idsTask.Any())
                {
                    _commonBusiness.DeleteId(CTask.TableName, idsTask);
                }
            }

            return base.Delete(JSonConvertUtil.Convert(campaignIds));
        }

        [HttpGet]
        public string GetCampaignTaskDetails(Guid idCampaign)
        {
            var result = _campaignBusiness.GetCampaignTaskDetails(idCampaign, ApplicationUserCurrent.AccountId);

            return JSonConvertUtil.Convert(result);
        }

        [HttpGet]
        public string GetCampaignTaskDetailsByMerchant(Guid idCampaign, Guid idMerchant)
        {
            var result = _campaignBusiness.GeCampaignTaskDetailsByMerchant(idCampaign, idMerchant, ApplicationUserCurrent.AccountId);

            return JSonConvertUtil.Convert(result);
        }

        [HttpGet]
        public string GetCampaignTasks(Guid idCampaign)
        {
            var listResult = _taskCampaignBusiness.GetAlltasksByCampaignId(idCampaign, ApplicationUserCurrent.AccountId);

            return JSonConvertUtil.Convert(listResult);
        }


        [HttpGet]
        public IActionResult TasksPerCampaign(string idCampaign, string filterValues, bool deleteFilter, string view, int pageIndex = 1, int pageSize = 10)
        {
            try
            {
                if (!string.IsNullOrEmpty(idCampaign))
                {
                    SetSessionVariable("idCampaign", idCampaign);
                }
                else
                {
                    idCampaign = GetSessionVariable("idCampaign");
                }

                if (!string.IsNullOrEmpty(view))
                {
                    SetSessionVariable("view", view);
                }
                else
                {
                    view = GetSessionVariable("view");
                }

                var id = Guid.Empty;
                if (!string.IsNullOrEmpty(idCampaign))
                {
                    id = Guid.Parse(Protector.Unprotect(idCampaign));
                }
                var filters = GetFilters(filterValues, deleteFilter);
                var tasks = _campaignBusiness.GetPaginatedTaskPerCampaignViewModel(id, pageIndex, pageSize, filters, ApplicationUserCurrent.AccountId);

                if (view == "list")
                {
                    return View("~/Views/Task/TaskList.cshtml", tasks);
                }

                return View(tasks);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }
        }

        [HttpGet]
        public IActionResult Geoposition(string campaign, string filterValues, bool deleteFilter)
        {
            try
            {
                if (!string.IsNullOrEmpty(campaign))
                {
                    SetSessionVariable("idCampaign", campaign);
                }
                else
                {
                    campaign = GetSessionVariable("idCampaign");
                }
                var filters = GetFilters(filterValues, deleteFilter);
                var ubicationList = _campaignBusiness.GetCampaignGeoposition(filters, campaign, ApplicationUserCurrent.AccountId, Protector);
                ubicationList.Properties.ControllerName = "Campaign";
                ubicationList.Properties.ActionName = "Geoposition";
                return View(ubicationList);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }
        }

        [HttpGet]
        public string GetActiveCampaignsList()
        {
            var listResult = _campaignBusiness.GetActiveCampaignsList(ApplicationUserCurrent.AccountId);

            return JSonConvertUtil.Convert(listResult);
        }

        [HttpGet]
        public IActionResult TaskPoll(Guid idTask)
        {
            try
            {
                ViewData[CTask.IdRegister] = idTask.ToString();

                ViewBag.StatusList = _statusTaskBusiness.GetAllStatusTasks()
                    .Select(s => new SelectListItem() { Text = s.Name, Value = s.Id.ToString() })
                    .ToList();

                ViewBag.ReasonsList =
                    _taskNotImplementedReasonBusiness.GetAllTaskNotImplementedReason()
                        .Select(t => new SelectListItem() { Value = t.Id.ToString(), Text = t.Name })
                        .ToList();

                return View();
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }
        }

        public IActionResult Index(string filterValues, bool deleteFilter, int pageSize = 20, int pageNumber = 1)
        {
            try
            {
                var filters = GetFilters(filterValues, deleteFilter);
                var campaigns = _campaignBusiness.GetPaginatedCampaigns(filters, pageSize, pageNumber, ApplicationUserCurrent.AccountId, Protector, _userId, _typeuser);
                return View(campaigns);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }
        }

        public IActionResult ImportBranches(string campaign, string filterValues, bool deleteFilter)
        {
            try
            {
                if (!string.IsNullOrEmpty(campaign))
                {
                    SetSessionVariable("idCampaign", campaign);
                }
                else
                {
                    campaign = GetSessionVariable("idCampaign");
                }
                var filters = GetFilters(filterValues, deleteFilter);
                var result = _campaignBusiness.ImportBranch(ApplicationUserCurrent.AccountId, filters);

                return View(result);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }
        }

        public IActionResult SelectBranches()
        {
            try
            {
                var filters = JSonConvertUtil.Deserialize<List<FilterValue>>(GetSessionVariable("filter"));
                var model = _campaignBusiness.GetBranchesSelected(ApplicationUserCurrent.AccountId, filters);
                return RedirectToAction("ImportBranches", model);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }
        }
    }
}

