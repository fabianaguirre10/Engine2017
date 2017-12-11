﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Mardis.Engine.Business.MardisCore;
using Mardis.Engine.Business.MardisSecurity;
using Mardis.Engine.DataAccess;
using Mardis.Engine.DataAccess.MardisCore;
using Mardis.Engine.Framework;
using Mardis.Engine.Framework.Resources.PagesConstants;
using Mardis.Engine.Web.Libraries.Security;
using Mardis.Engine.Web.Libraries.Services;
using Mardis.Engine.Web.Libraries.Util;
using Mardis.Engine.Web.Model;
using Mardis.Engine.Web.ViewModel.TaskViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Mardis.Engine.Web.App_code;

namespace Mardis.Engine.Web.Controllers
{
    [Authorize]
    public class TaskController : AController<TaskController>
    {
        #region Variables y Constructores

        private readonly TaskCampaignBusiness _taskCampaignBusiness;
        private readonly StatusTaskBusiness _statusTaskBusiness;
        private readonly TaskNotImplementedReasonBusiness _taskNotImplementedReasonBusiness;
        private readonly BranchImageBusiness _branchImageBusiness;
        private readonly ILogger<TaskController> _logger;
        private readonly Guid _idAccount;
        private readonly IDataProtector _protector;
        private readonly UserBusiness _userBusiness;
        private readonly Guid _userId;
        private readonly QuestionBusiness _questionBusiness;
        private readonly QuestionDetailBusiness _questionDetailBusiness;
        private readonly IMemoryCache _cache;
        private readonly ServiceBusiness _serviceBusiness;

        public TaskController(UserManager<ApplicationUser> userManager,
                                IHttpContextAccessor httpContextAccessor,
                                MardisContext mardisContext,
                                ILogger<TaskController> logger,
                                ILogger<ServicesFilterController> loggeFilter,
                                    IDataProtectionProvider protectorProvider,
                                    IMemoryCache memoryCache,
                                    RedisCache distributedCache)
            : base(userManager, httpContextAccessor, mardisContext, logger)
        {
            _protector = protectorProvider.CreateProtector(GetType().FullName);
            _logger = logger;
            ControllerName = CTask.Controller;
            TableName = CTask.TableName;
            _taskCampaignBusiness = new TaskCampaignBusiness(mardisContext, distributedCache);
            _statusTaskBusiness = new StatusTaskBusiness(mardisContext, distributedCache);
            _taskNotImplementedReasonBusiness = new TaskNotImplementedReasonBusiness(mardisContext);
            _branchImageBusiness = new BranchImageBusiness(mardisContext);
            _userBusiness = new UserBusiness(mardisContext);
            _questionBusiness = new QuestionBusiness(mardisContext);
            _questionDetailBusiness = new QuestionDetailBusiness(mardisContext);
            _cache = memoryCache;
            _serviceBusiness = new ServiceBusiness(mardisContext);

            if (ApplicationUserCurrent.UserId != null)
            {
                _userId = new Guid(ApplicationUserCurrent.UserId);
                Global.UserID = _userId;
                Global.AccountId = ApplicationUserCurrent.AccountId;
                Global.ProfileId = ApplicationUserCurrent.ProfileId;
                Global.PersonId = ApplicationUserCurrent.PersonId;
            }

            _idAccount = ApplicationUserCurrent.AccountId;
        }

        #endregion

        #region Acciones

        [HttpGet]
        public IActionResult Register(Guid idTask, Guid? idCampaign = null, string returnUrl = null)
        {

            try
            {
                ViewData["ReturnUrl"] = returnUrl;
                var model = _taskCampaignBusiness.GetTask(idCampaign, idTask, ApplicationUserCurrent.AccountId);
                model.ReturnUrl = returnUrl;

                GetMerchants();

                return View(model);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }
        }
        /// <summary>
        /// funcion para obtener la pregunta enlazada
        /// </summary>
        [HttpGet]
        public QuestionDetail Obtnerpregunta(Guid id)
        {
            return _questionDetailBusiness.GetOne(id);
        }

        [HttpPost]
        public IActionResult Register(TaskRegisterViewModel model, string returnUrl = null)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    GetMerchants();
                    return View(model);
                }

                _taskCampaignBusiness.Save(model, ApplicationUserCurrent.AccountId);

                if (!string.IsNullOrEmpty(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }
        }

        private void GetMerchants()
        {
            ViewBag.Merchants =
                _userBusiness.GetUserListByType(CTypePerson.PersonMerchant, ApplicationUserCurrent.AccountId)
                    .Select(c => new SelectListItem() { Text = c.Person.Name + " " + c.Person.SurName, Value = c.Id.ToString() })
                    .ToList();
        }

        [HttpGet]
        public IActionResult Index(string pidCampaign, string filterValues, bool deleteFilter, int pageIndex = 1, int pageSize = 10)
        {
            try
            {
                if (!string.IsNullOrEmpty(pidCampaign))
                {
                    SetSessionVariable("pidCampaign", pidCampaign);
                }
                else
                {
                    pidCampaign = GetSessionVariable("pidCampaign");
                }

                var filters = GetFilters(filterValues, deleteFilter);
                var idCampaign = Guid.Parse(CampaignController.Protector.Unprotect(pidCampaign));
                var idAccount = ApplicationUserCurrent.AccountId;
                var model = _taskCampaignBusiness.GetPaginatedTasksList(idCampaign, idAccount, filters, pageIndex, pageSize);

                return View(model);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }
        }

        public IActionResult MyTasks(string filterValues, bool deleteFilter, string view, int pageIndex = 1, int pageSize = 15)
        {
            try
            {
                var filters = GetFilters(filterValues, deleteFilter);

                if (!string.IsNullOrEmpty(view))
                {
                    SetSessionVariable("view", view);
                }
                else
                {
                    view = GetSessionVariable("view");
                }

                if (ApplicationUserCurrent.UserId == null)
                {
                    ApplicationUserCurrent.UserId = Convert.ToString(Global.UserID);
                    ApplicationUserCurrent.AccountId = Global.AccountId;
                    ApplicationUserCurrent.ProfileId = Global.ProfileId;
                    ApplicationUserCurrent.PersonId = Global.PersonId;
                }
                var model = _taskCampaignBusiness.GetTasksPerCampaign(Guid.Parse(ApplicationUserCurrent.UserId), pageIndex, pageSize, filters, ApplicationUserCurrent.AccountId);

                if (view == "list")
                {
                    return View("~/Views/Task/TaskList.cshtml", model);
                }

                return View(model);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = e.Message });
            }
        }

        [HttpGet]
        public IActionResult Profile(Guid idTask)
        {
            try
            {
                ViewData[CTask.IdRegister] = idTask.ToString();
                LoadSelectItems();

                return View();
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }

        }

        [HttpGet]
        public JsonResult Get(Guid idTask)
        {
            try
            {
                var model = _taskCampaignBusiness.GetSectionsPoll(idTask, _idAccount);
                JSonConvertUtil.Convert(model);
                return Json(model);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return null;
            }
        }

        [HttpPost]
        public JsonResult Save(string task)
        {
            try
            {
                var model = JSonConvertUtil.Deserialize<MyTaskViewModel>(task);

                if (model == null)
                {
                    return null;
                }
                if (ApplicationUserCurrent.UserId == null)
                {
                    ApplicationUserCurrent.UserId = Convert.ToString(Global.UserID);
                    ApplicationUserCurrent.AccountId = Global.AccountId;
                    ApplicationUserCurrent.ProfileId = Global.ProfileId;
                    ApplicationUserCurrent.PersonId = Global.PersonId;
                }
                _taskCampaignBusiness.SaveAnsweredPoll(model, ApplicationUserCurrent.AccountId, ApplicationUserCurrent.ProfileId, Guid.Parse(ApplicationUserCurrent.UserId));

                return Json("OK");
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return null;
            }
        }

        #endregion

        public void LoadSelectItems()
        {
            ViewBag.StatusList = _statusTaskBusiness.GetAllStatusTasks()
                .Select(s => new SelectListItem() { Text = s.Name, Value = s.Id.ToString() })
                    .ToList();

            ViewBag.ReasonsList =
                _taskNotImplementedReasonBusiness.GetAllTaskNotImplementedReason()
                    .Select(t => new SelectListItem() { Value = t.Id.ToString(), Text = t.Name })
                    .ToList();
        }

        [HttpGet]
        public List<BranchImages> GetBranchImagesList(Guid branchId)
        {
            try
            {
                var resultList = _branchImageBusiness.GetBranchesImagesList(branchId, ApplicationUserCurrent.AccountId);
                return resultList;
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return null;
            }
        }

        [HttpGet]
        public List<StatusTask> GetAllStatusTask()
        {
            return _statusTaskBusiness.GetAllStatusTasks();
        }

        [HttpGet]
        public List<TaskNoImplementedReason> GetAllTaskNotImplementedReason()
        {
            return _taskNotImplementedReasonBusiness.GetAllTaskNotImplementedReason();
        }

        [HttpPost]
        public string SaveTaskRegister(string inputTask)
        {

            var taskCampaign = _taskCampaignBusiness.SaveTaskRegister(inputTask, ApplicationUserCurrent.AccountId);

            return JSonConvertUtil.Convert(taskCampaign);
        }

        [HttpGet]
        public string GetTaskListByServiceAndBranch(Guid idBranch, Guid idService)
        {
            var listResult = _taskCampaignBusiness.GetTaskListByServiceAndBranch(idBranch, idService, ApplicationUserCurrent.AccountId);

            return JSonConvertUtil.Convert(listResult);
        }

        [HttpPost]
        public JsonResult AddSection(string task, string idSection)
        {
            var model = JSonConvertUtil.Deserialize<MyTaskViewModel>(task);
            //  _taskCampaignBusiness.AddSection(model, ApplicationUserCurrent.AccountId, ApplicationUserCurrent.ProfileId, Guid.Parse(ApplicationUserCurrent.UserId), Guid.Parse(idSection));
            //model = _taskCampaignBusiness.GetSectionsPoll(model.IdTask, _idAccount,idSection);
            //return RedirectToAction("Profile", new { idTask = model.IdTask });
            return Json(model);
        }
        [HttpPost]
        public IActionResult DuplicateSection(MyTaskViewModel model)
        {

            //if (!ModelState.IsValid)
            //{
            //    return RedirectToAction("Profile", new { idTask = model.IdTask });
            //}
            // _serviceBusiness.DuplicateSection(model, ApplicationUserCurrent.AccountId);
            return RedirectToAction("Register", new { idTask = model.IdTask });
        }

        #region Imágenes

        [HttpPost]
        public async Task<bool> UploadBranchImage(string fileName, string type)
        {
            var idBranch = Guid.Parse(fileName.Split('_')[1]);

            fileName += @"@" + type + @"@" + DateTime.Now.ToString("yyyyMMdd") + @"@" + DateTime.Now.ToString("HHmmss") +
                        ".jpg";

            try
            {
                if (idBranch == Guid.Empty)
                {
                    throw new ExceptionMardis();
                }

                var files = Request.Form.Files;

                await UploadFilesToAzure(idBranch.ToString(), files, fileName);

            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        [HttpGet]
        public string DeleteImageBranch(Guid idImageBranch)
        {
            var imageBranch = _branchImageBusiness.GetBranchImageById(idImageBranch, ApplicationUserCurrent.AccountId);
            AzureStorageUtil.DeleteBlob(CBranch.ImagesContainer, imageBranch.NameFile);
            _branchImageBusiness.DeleteBranchImage(idImageBranch, ApplicationUserCurrent.AccountId);

            var itemResult = _branchImageBusiness.GetBranchesImagesList(imageBranch.IdBranch, ApplicationUserCurrent.AccountId);

            return JSonConvertUtil.Convert(itemResult);
        }

        private async Task UploadFilesToAzure(string branch, IFormFileCollection files, string fileName)
        {
            foreach (var file in files)
            {

                byte[] bytes;
                using (var stream = file.OpenReadStream())
                {
                    bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, (int)stream.Length);
                }

                var fileStream = (new MemoryStream(bytes));

                await AzureStorageUtil.UploadFromStream(fileStream, CBranch.ImagesContainer, fileName);
                var uri = AzureStorageUtil.GetUriFromBlob(CBranch.ImagesContainer, fileName);
                _branchImageBusiness.SaveBranchImages(new BranchImages()
                {
                    IdBranch = new Guid(branch),
                    NameContainer = CBranch.ImagesContainer,
                    NameFile = fileName,
                    UrlImage = uri
                });
            }
        }

        #endregion

        [HttpPost]
        public bool ChangeStatusNotImplemented(Guid idTask, Guid idReason, string comment)
        {
            try
            {
                var myTask = _taskCampaignBusiness.GetTaskByIdForRegisterPage(idTask, ApplicationUserCurrent.AccountId);
                var status = _statusTaskBusiness.GeStatusTaskByName(CTask.StatusNotImplemented);
                myTask.IdTaskNoImplementedReason = idReason;
                myTask.IdStatusTask = status.Id;
                myTask.CommentTaskNoImplemented = comment;
                myTask.DateModification = DateTime.Now;
                _taskCampaignBusiness.ModifyTask(myTask);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception.Message);
                return false;
            }
            return true;
        }

    }
}