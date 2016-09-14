using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace IdentityPasswordExpiration.Filters
{
    #region PQ Change
    public class AuthorizePasswordCanExpiereAttribute : AuthorizeAttribute
    {
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            IPrincipal user = filterContext.HttpContext.User;
            if(user!=null && user.Identity.IsAuthenticated)
            {
                var identity = (ClaimsPrincipal)Thread.CurrentPrincipal;
                var stingLastPasswordChangedDateUtc = identity.Claims.Where(c => c.Type == "LastPasswordChangedDateUtc").Select(c => c.Value).SingleOrDefault();

                DateTime LastPasswordChangedDateUtc;
                if (!DateTime.TryParse(stingLastPasswordChangedDateUtc, out LastPasswordChangedDateUtc))
                {
                    // handle parse failure
                }

                int PasswordExpirationAfterDays = 90;
                TimeSpan timespan = DateTime.UtcNow - LastPasswordChangedDateUtc;
                int PasswordWillExpiere = (int)(PasswordExpirationAfterDays - timespan.TotalSeconds);
                //int PasswordWillExpiere = (int)PasswordExpirationAfterDays - timespan.TotalDays;

                if (PasswordWillExpiere <= 0)
                {         
                    filterContext.Result = new RedirectToRouteResult(
                       new RouteValueDictionary
                       {
                            { "controller", "Manage" },
                            { "action", "ChangePassword" },
                            { "reason","Your password has expiered. Please change your password."}                         
                       });
                }
                else
                {
                   filterContext.Controller.ViewData.Add("PasswordWillExpiere", PasswordWillExpiere); 
                }
            }

            base.OnAuthorization(filterContext);    
        }
    }
    #endregion PQ Change
}