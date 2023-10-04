using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace eShopLegacyMVC.Models
{
    public class ApplicationUser : IdentityUser
    {
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }

        private int? _zipCode = null; 

        public int? ZipCode
        {
            get
            {
                if (_zipCode is null)
                {
                    var uri = string.Format("http://10.0.0.42/UserLookup.svc/zipCode?id={0}", Id);
                    var req = HttpWebRequest.Create(uri) as HttpWebRequest;
                    req.Method = "GET";
                    req.ServicePoint.Expect100Continue = false;

                    var response = req.GetResponse();
                    var responseStream = response.GetResponseStream();
                    using (var reader = new StreamReader(responseStream))
                    {
                        var zipCode = reader.ReadToEnd();
                        _zipCode = int.Parse(zipCode);
                    }
                }
                return _zipCode;
            }
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("IdentityDBContext", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }
    }
}