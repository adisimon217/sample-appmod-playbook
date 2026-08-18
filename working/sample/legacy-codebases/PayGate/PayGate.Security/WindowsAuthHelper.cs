using System;
using System.Security.Principal;
using System.Web;
using System.Web.Http.Controllers;

namespace PayGate.Security
{
    /// <summary>
    /// Helper utilities for Windows Authentication on admin endpoints.
    /// PayGate admin API is accessible only to members of PAYGATE\PayGate-Admins group.
    /// </summary>
    public static class WindowsAuthHelper
    {
        private const string AdminGroupName = "PAYGATE\\PayGate-Admins";
        private const string OperatorsGroupName = "PAYGATE\\PayGate-Operators";

        /// <summary>
        /// Get the current Windows-authenticated user identity.
        /// </summary>
        public static string GetCurrentWindowsUser(HttpRequestContext requestContext)
        {
            var principal = requestContext?.Principal as WindowsPrincipal;
            if (principal?.Identity is WindowsIdentity identity)
            {
                return identity.Name;
            }

            // Fallback: try HttpContext
            var httpContext = HttpContext.Current;
            if (httpContext?.User?.Identity is WindowsIdentity windowsIdentity)
            {
                return windowsIdentity.Name;
            }

            return null;
        }

        /// <summary>
        /// Check if the current user is a PayGate administrator.
        /// </summary>
        public static bool IsAdmin(HttpRequestContext requestContext)
        {
            var principal = requestContext?.Principal as WindowsPrincipal;
            if (principal == null) return false;

            return principal.IsInRole(AdminGroupName);
        }

        /// <summary>
        /// Check if the current user is a PayGate operator (read-only access).
        /// </summary>
        public static bool IsOperator(HttpRequestContext requestContext)
        {
            var principal = requestContext?.Principal as WindowsPrincipal;
            if (principal == null) return false;

            return principal.IsInRole(OperatorsGroupName) || principal.IsInRole(AdminGroupName);
        }

        /// <summary>
        /// Get the SID of the authenticated user (for audit logging).
        /// </summary>
        public static string GetUserSid(HttpRequestContext requestContext)
        {
            var principal = requestContext?.Principal as WindowsPrincipal;
            if (principal?.Identity is WindowsIdentity identity)
            {
                return identity.User?.Value; // SID string
            }
            return null;
        }
    }
}
