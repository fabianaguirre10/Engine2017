using System.Linq;
using Mardis.Engine.Business;
using Mardis.Engine.Business.MardisCore;
using Mardis.Engine.Business.MardisSecurity;
using Mardis.Engine.DataAccess;
using Mardis.Engine.Web.Libraries.Security;
using Mardis.Engine.Web.Model;
using Mardis.Engine.Web.Services;
using Mardis.Engine.Web.ViewModel.DashBoardViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;

namespace Mardis.Engine.Web.Controllers
{

    [Authorize]
    public class HomeController : AController<HomeController>
    {
        private readonly MenuBusiness _menuBusiness;
        private readonly CampaignBusiness _campaignBusiness;
        private readonly HomeBusiness _homeBusiness;
        private readonly IDataProtector _protector;
        private readonly IDataProtector _protectorCampaign;

        public HomeController(UserManager<ApplicationUser> userManager,
                              IHttpContextAccessor httpContextAccessor,
                              MardisContext mardisContext,
                              ILogger<HomeController> logger,
                              IMenuService menuService,
                                    IDataProtectionProvider protectorProvider
                              )
            : base(userManager, httpContextAccessor, mardisContext, logger)
        {
            _protector = protectorProvider.CreateProtector(GetType().FullName);
            _protectorCampaign = protectorProvider.CreateProtector("Mardis.Engine.Web.Controllers.CampaignController");
            _menuBusiness = new MenuBusiness(mardisContext);
            _campaignBusiness = new CampaignBusiness(mardisContext);
            _homeBusiness = new HomeBusiness(mardisContext);
        }
        
        //[HttpPost(Name = "GetMenuByProfile")]
        //public IEnumerable GetMenuByProfile()
        //{
        //    var idProfile = ApplicationUserCurrent.ProfileId;
        //    var itemMenu =
        //    _menuBusiness.GetOnlyParentsByProfile(idProfile);

        //    foreach (var itemMenuTemp in itemMenu)
        //    {
        //        itemMenuTemp.MenuChildrens = _menuBusiness.GetChildrens(idProfile, itemMenuTemp.Id);
        //    }

        //    return itemMenu;login
        //}
        
        public IActionResult Index()
        {
            return View();
        }
        
        [HttpGet]
        public IActionResult DashBoard(DashBoardViewModel model, string filterValues, bool deleteFilter, int pageIndex = 1, int pageSize = 10)
        {

            if (!string.IsNullOrEmpty(model.IdCampaign))
            {
                SetSessionVariable("idCampaign", model.IdCampaign);
            }
            else if (!string.IsNullOrEmpty(filterValues) || pageIndex != 1)
            {
                model.IdCampaign = GetSessionVariable("idCampaign");
            }

            ViewBag.CampaignList =
                _campaignBusiness.GetActiveCampaignsList(ApplicationUserCurrent.AccountId)
                    .Select(c => new SelectListItem() { Value = _protectorCampaign.Protect(c.Id.ToString()), Text = c.Name });

            var filters = GetFilters(filterValues, deleteFilter);

            model = _homeBusiness.GetDashBoard(model, filters, pageIndex, pageSize, ApplicationUserCurrent.AccountId, _protectorCampaign);

            if (!string.IsNullOrEmpty(model.IdCampaign))
            {
                model.IdCampaign = _protectorCampaign.Protect(model.IdCampaign);
            }

            return View(model);
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
