# IdentityPasswordExpiration

Add password expiration to ASP.Net Identity 2.2.1

Make a new project. Use ASP.NET WebApplication (.NET Framework) 


![Image01](https://raw.githubusercontent.com/PeterQuistgaard/IdentityPasswordExpiration/master/image01.png)



![Image02](https://raw.githubusercontent.com/PeterQuistgaard/IdentityPasswordExpiration/master/image02.png)

All changes is marked with "region PQ Change"

###Change *IdentitityModels.cs* (In folder Models)

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
