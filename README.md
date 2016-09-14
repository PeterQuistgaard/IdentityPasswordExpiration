# IdentityPasswordExpiration

Add password expiration to ASP.Net Identity 2.2.1

Make a new project. Use ASP.NET WebApplication (.NET Framework) 


![Image01](https://raw.githubusercontent.com/PeterQuistgaard/IdentityPasswordExpiration/master/image01.png)



![Image02](https://raw.githubusercontent.com/PeterQuistgaard/IdentityPasswordExpiration/master/image02.png)

All changes is marked with "region PQ Change"

###**IdentitityModels.cs** (folder Models)

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

**IdentityConfig.cs** (folder App_Start)
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

**x** (folder y)
```C#


```
