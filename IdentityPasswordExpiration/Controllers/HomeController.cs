using IdentityPasswordExpiration.Filters;
using IdentityPasswordExpiration.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace IdentityPasswordExpiration.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        #region PQ Change
        [AuthorizePasswordCanExpiere]
        #endregion PQ Change
        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        #region PQ Change
        [Authorize]
        #endregion PQ Change
        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}