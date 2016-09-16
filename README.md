# IdentityPasswordExpiration

Add password expiration to ASP.Net Identity 2.2.1

## Resume
- Add new property ```LastPasswordChangedDateUtc``` to ```ApplicationUser``` and add it to Claims.
- Change ```IdentityConfig.cs``` to update ```LastPasswordChangedDateUtc``` when password is changed or reset.  
- Create new authorize filter, to check when password has expiered.

## How to create the solution 
Create a new project. Use ASP.NET WebApplication (.NET Framework) 


![Image01](https://raw.githubusercontent.com/PeterQuistgaard/IdentityPasswordExpiration/master/image01.png)



![Image02](https://raw.githubusercontent.com/PeterQuistgaard/IdentityPasswordExpiration/master/image02.png)

 
## Make some changes in the generated code.

All changes are placed between ```#region PQ Change``` and ```#endregion PQ Change```.   

### IdentitityModels.cs (folder Models)

```C#

    public class ApplicationUser : IdentityUser
    {
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);

            // Add custom user claims here
            #region PQ Change
            userIdentity.AddClaim(new Claim("LastPasswordChangedDateUtc", (this.LastPasswordChangedDateUtc + "" ?? "")));
            #endregion  PQ Change
            
            return userIdentity;
        }

        #region PQ Change
        public DateTime? LastPasswordChangedDateUtc { get; set; }
        #endregion PQ Change
    }
    
```

### Web.Config
Change connectionStrings to match your prefered database. 
     
```XML

  <connectionStrings>
   <!-- #region PQ Change -->
    <add name="DefaultConnection" 
         providerName="System.Data.SqlClient" 
         connectionString="Data Source=.\SQLEXPRESS;Initial Catalog=IdentityPasswordExpiration;Integrated Security=SSPI" />
   <!-- #endregion PQ Change-->      
  </connectionStrings>

```


### IdentityConfig.cs (folder App_Start)
Update IdentityConfig.cs to update ```LastPasswordChangedDateUtc``` when password is changed or reset.  


```C#

   public class ApplicationUserManager : UserManager<ApplicationUser>
    {
        public ApplicationUserManager(IUserStore<ApplicationUser> store)
            : base(store)
        {
        }

        public static ApplicationUserManager Create(IdentityFactoryOptions<ApplicationUserManager> options, IOwinContext context) 
        {
            var manager = new ApplicationUserManager(new UserStore<ApplicationUser>(context.Get<ApplicationDbContext>()));
            // Configure validation logic for usernames
            manager.UserValidator = new UserValidator<ApplicationUser>(manager)
            {
                AllowOnlyAlphanumericUserNames = false,
                RequireUniqueEmail = true
            };

            // Configure validation logic for passwords
            manager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 6,
                RequireNonLetterOrDigit = true,
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true,
            };

            // Configure user lockout defaults
            manager.UserLockoutEnabledByDefault = true;
            manager.DefaultAccountLockoutTimeSpan = TimeSpan.FromMinutes(5);
            manager.MaxFailedAccessAttemptsBeforeLockout = 5;

            // Register two factor authentication providers. This application uses Phone and Emails as a step of receiving a code for verifying the user
            // You can write your own provider and plug it in here.
            manager.RegisterTwoFactorProvider("Phone Code", new PhoneNumberTokenProvider<ApplicationUser>
            {
                MessageFormat = "Your security code is {0}"
            });
            manager.RegisterTwoFactorProvider("Email Code", new EmailTokenProvider<ApplicationUser>
            {
                Subject = "Security Code",
                BodyFormat = "Your security code is {0}"
            });
            manager.EmailService = new EmailService();
            manager.SmsService = new SmsService();
            var dataProtectionProvider = options.DataProtectionProvider;
            if (dataProtectionProvider != null)
            {
                manager.UserTokenProvider = 
                    new DataProtectorTokenProvider<ApplicationUser>(dataProtectionProvider.Create("ASP.NET Identity"));
            }
            return manager;
        }


        # region PQ Change
        public override async Task<IdentityResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
        {
            var result = await base.ChangePasswordAsync(userId, currentPassword, newPassword);
            if (result.Succeeded)
            {
                ApplicationUser myApplicationUser = await Store.FindByIdAsync(userId);
                myApplicationUser.LastPasswordChangedDateUtc = DateTime.UtcNow;
                await Store.UpdateAsync(myApplicationUser);
            }
            return result;
        }
        #endregion PQ Change

        #region PQ Change
        public override async Task<IdentityResult> ResetPasswordAsync(string userId, string currentPassword, string newPassword)
        {
            var result = await base.ResetPasswordAsync(userId, currentPassword, newPassword);
            if (result.Succeeded)
            {
                ApplicationUser myApplicationUser = await Store.FindByIdAsync(userId);
                myApplicationUser.LastPasswordChangedDateUtc = DateTime.UtcNow;
                await Store.UpdateAsync(myApplicationUser);
            }
            return result;
        }
        #endregion PQ Change

    }

```

### AccountController.cs (folder Controllers)

Add ```user.LastPasswordChangedDateUtc = DateTime.UtcNow;``` to set date when user is created.  
```C#

       [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                #region PQ Change
                user.LastPasswordChangedDateUtc = DateTime.UtcNow;
                #endregion PQ Change
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await SignInManager.SignInAsync(user, isPersistent:false, rememberBrowser:false);
                    
                    // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                    // Send an email with this link
                    // string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    // await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

                    return RedirectToAction("Index", "Home");
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

```



### AuthorizePasswordCanExpiere.cs (folder Filters)
Create new authorize filter, to check when password is expiered. The new ```AuthorizePasswordCanExpiereAttribute``` inherit from ```AuthorizeAttribute```.  
- Add new folder *Filters*  
- Add new class *AuthorizePasswordCanExpiere.cs*

```C#

namespace IdentityPasswordExpiration.Filters
{
    #region PQ Change
    public class AuthorizePasswordCanExpiereAttribute : AuthorizeAttribute
    {
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            var user = ClaimsPrincipal.Current;
            if(user!=null && user.Identity.IsAuthenticated)
            { 
                var stingLastPasswordChangedDateUtc = user.FindFirst("LastPasswordChangedDateUtc").Value;

                DateTime LastPasswordChangedDateUtc;
                if (!DateTime.TryParse(stingLastPasswordChangedDateUtc, out LastPasswordChangedDateUtc))
                {
                    // handle parse failure
                }

                int PasswordExpirationAfterDays = 90;
                TimeSpan timespan = DateTime.UtcNow - LastPasswordChangedDateUtc;
                int PasswordWillExpiere = (int)(PasswordExpirationAfterDays - timespan.TotalSeconds);//Only when testing
                //int PasswordWillExpiere = (int)PasswordExpirationAfterDays - timespan.TotalDays;//Use in production

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

```


### HomeController.cs (folder Controllers)
To check the solution, decorate "ActionResult About" with the new attribute ```[AuthorizePasswordCanExpiere]```.   
And decorate "ActionResult Contact" with the standard attribute ```[Authorize]```  

```C#

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
   
```

### About.cshtml (folder Views\Home)
Use ```@ViewData["PasswordWillExpiere"]``` from the filter AuthorizePasswordCanExpiereAttribute to inform the user.

Remark: Seconds is used as days - just when testing. 

```cshtml

@{
    ViewBag.Title = "About";
}
<h2>@ViewBag.Title.</h2>
<hr/>

<!-- #region PQ Change -->
<h3>Your password will expiere in @ViewData["PasswordWillExpiere"] days.</h3>
@if ((int)ViewData["PasswordWillExpiere"] < 40)
{
    <div class="alert alert-danger">Consider to change your password.</div>
}
<!-- #endregion PQ Change-->

```

----------------------------
**Screenshot:**  
![Image03](https://raw.githubusercontent.com/PeterQuistgaard/IdentityPasswordExpiration/master/image03.png)



### ManageController.cs (folder Controllers)
Get the parameter  "reason" and send it to View with ViewBag.reason.
      
```C#

        public ActionResult ChangePassword(string reason)
        {
            #region PQ Change
            if (reason != null) { ViewBag.reason= reason;}
            #endregion PQ Change

            return View();
        }

```

### ChangePassword.cshtml (folder Views\Manage)
```cshtml

<h2>@ViewBag.Title.</h2>

<!-- #region PQ Change -->
    @if (ViewBag.reason != null)
    {
        <div class="alert alert-danger">@ViewBag.reason</div>
    }
<!-- #endregion PQ Change-->

```
The parameter "reason" gives a reason, why the user need to change password.  

-------------------------------  
**Screenshot:**  
![Image04](https://raw.githubusercontent.com/PeterQuistgaard/IdentityPasswordExpiration/master/image04.png)

Suggestions for improvement: Create Validator to force user not to reuse old passwords. Tjek if "New passwod" is not equal to "Current password".   
