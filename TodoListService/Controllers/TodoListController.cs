using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using TodoListService.Models;

namespace TodoListService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TodoListController : ControllerBase
    {
        // In-memory TodoList
        private static readonly Dictionary<int, TD> TodoStore = new Dictionary<int, TD>();

        // This is needed to get access to the internal HttpContext.User, if available.
        private readonly IHttpContextAccessor _contextAccessor;

        private const string _todoListReadScope = "ToDoList.Read";
        private const string _todoListReadWriteScope = "ToDoList.ReadWrite";
        private const string _todoListReadAllPermission = "ToDoList.Read.All";
        private const string _todoListReadWriteAllPermission = "ToDoList.ReadWrite.All";

        /// <summary>
        /// We store the object id of the user/app derived from the presented Access token
        /// </summary>
        private string _currentPrincipalId = string.Empty;

        /// <summary>
        /// The API controller that manages an instance of ToDo list
        /// </summary>
        /// <param name="contextAccessor"></param>
        public TodoListController(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;

            /**
             * When the Access Token belongs to a user (signing-in to a client app), the following claims should
             * help API developer further fine tune their business logic
             *
             * The 'oid' (object id) is the only claim that should be used to uniquely identify
             * a user in an Azure AD tenant. The token might have one or more of the following claim,
             * that might seem like a unique identifier, but is not and should not be used as such,
             * especially for systems which act as system of record (SOR):
             *
             * - upn (user principal name): might be unique amongst the active set of users in a tenant but
             * tend to get reassigned to new employees as employees leave the organization and
             * others take their place or might change to reflect a personal change like marriage.
             *
             * - email: might be unique amongst the active set of users in a tenant but tend to get
             * reassigned to new employees as employees leave the organization and others take their place.
             *
             * - acct: Helps distinguish homed versus guest users
             **/

            /**
             * When the Access Token belongs to an application signing-in as itself (using client credentials flow/SP authN, app-only AuthN),
             * the following claims are of interest
             *
             * The 'oid' (object id) contains the id of the service principal of the client app. This value is immutable,
             *
             * - idtyp : helps distinguish tokens obtained using app-only flow, not included in tokens issued to users.
             * - "azp" (or "appid" in v1 tokens) - the client/app id of the client application
             **/

            // We seek the details of the user/app represented by the access token presented to this API, This can be empty unless authN succeeded
            // If a user signed-in, the value will be the unique identifier of the user.
            _currentPrincipalId = GetCurrentClaimsPrincipal()?.GetObjectId();

            if (!IsAppOnlyToken() && !string.IsNullOrWhiteSpace(_currentPrincipalId))
            {
                // Pre-populate with sample data
                if (TodoStore.Count == 0 && !string.IsNullOrEmpty(_currentPrincipalId))
                {
                    TodoStore.Add(1, new TD() { Id = 1, Owner = $"{_currentPrincipalId}", Title = "Pick up groceries" });
                    TodoStore.Add(2, new TD() { Id = 2, Owner = $"{_currentPrincipalId}", Title = "Finish invoice report" });
                }
            }
        }

        /// <summary>
        /// Indicates of the AT presented was for an app-only token or not.
        /// </summary>
        /// <returns></returns>
        private bool IsAppOnlyToken()
        {
            // Add in the optional 'idtyp' claim to check if the access token is coming from an application or user.
            //
            // See: https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-optional-claims

            if (GetCurrentClaimsPrincipal() != null)
            {
                return GetCurrentClaimsPrincipal().Claims.Any(c => c.Type == "idtyp" && c.Value == "app");
            }

            return false;
        }

        /// <summary>
        /// returns the current claimsPrincipal (user/Client app) dehydrated from the Access token
        /// </summary>
        /// <returns></returns>
        private ClaimsPrincipal GetCurrentClaimsPrincipal()
        {
            // Irrespective of whether a user signs in or not, the AspNet security middle-ware dehydrates the claims in the
            // HttpContext.User.Claims collection

            if (_contextAccessor.HttpContext != null && _contextAccessor.HttpContext.User != null)
            {
                return _contextAccessor.HttpContext.User;
            }

            return null;
        }

        // GET: api/values
        /// <summary>
        /// Returns todo list items in a list
        /// </summary>
        /// <returns></returns>
        [HttpGet()]
        [RequiredScopeOrAppPermission(
            AcceptedScope = new string[] { _todoListReadScope, _todoListReadWriteScope },
            AcceptedAppPermission = new string[] { _todoListReadAllPermission, _todoListReadWriteAllPermission }
            )]
        public IEnumerable<Todo> Get()
        {
            if (!IsAppOnlyToken())
            {
                // this is a request for all ToDo list items of a certain user.
                return (IEnumerable<Todo>)TodoStore.Values.Where(x => x.Owner == _currentPrincipalId);
            }
            return null;
          
        }

        

        


       
       
    }
}


