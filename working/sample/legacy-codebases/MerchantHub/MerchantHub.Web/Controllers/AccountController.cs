using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using log4net;
using MerchantHub.Core;
using MerchantHub.Data.Repositories;

namespace MerchantHub.Web.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(AccountController));

        // GET: /Account/Login
        public ActionResult Login(string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password, bool rememberMe = false, string returnUrl = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    ModelState.AddModelError("", "Username and password are required.");
                    return View();
                }

                if (Membership.ValidateUser(username, password))
                {
                    // Get merchant info for session
                    var merchantRepo = ServiceLocator.Resolve<IMerchantRepository>();
                    var user = merchantRepo.GetUserByUsername(username);

                    if (user == null || !user.IsActive)
                    {
                        ModelState.AddModelError("", "Account is inactive. Contact support.");
                        _log.WarnFormat("Inactive user login attempt: {0}", username);
                        return View();
                    }

                    // Create forms auth ticket
                    FormsAuthentication.SetAuthCookie(username, rememberMe);

                    // Store merchant info in session
                    Session["CurrentMerchantId"] = user.MerchantId;
                    Session["CurrentUserId"] = user.UserId;
                    Session["UserDisplayName"] = user.DisplayName;
                    Session["UserRole"] = user.Role;
                    Session["MerchantName"] = user.MerchantName;
                    Session["LoginTime"] = DateTime.Now;

                    // Update last login
                    merchantRepo.UpdateLastLogin(user.UserId, Request.UserHostAddress);

                    _log.InfoFormat("User login successful: {0} (Merchant: {1}) from {2}",
                        username, user.MerchantId, Request.UserHostAddress);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    return RedirectToAction("Dashboard", "Merchant");
                }
                else
                {
                    _log.WarnFormat("Failed login attempt for user: {0} from IP: {1}",
                        username, Request.UserHostAddress);
                    ModelState.AddModelError("", "Invalid username or password.");
                    return View();
                }
            }
            catch (Exception ex)
            {
                _log.Error("Error during login", ex);
                ModelState.AddModelError("", "An error occurred. Please try again.");
                return View();
            }
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult Logout()
        {
            var username = User.Identity.Name;
            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();

            _log.InfoFormat("User logged out: {0}", username);
            return RedirectToAction("Login");
        }

        // GET: /Account/ForgotPassword
        public ActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(string email)
        {
            try
            {
                var merchantRepo = ServiceLocator.Resolve<IMerchantRepository>();
                var user = merchantRepo.GetUserByEmail(email);

                // Always show success message to prevent email enumeration
                TempData["Success"] = "If an account with that email exists, a reset link has been sent.";

                if (user != null)
                {
                    // Generate reset token
                    var token = Guid.NewGuid().ToString("N");
                    merchantRepo.SetPasswordResetToken(user.UserId, token, DateTime.Now.AddHours(24));

                    // Send email - NOTE: using System.Net.Mail directly, no email service
                    var resetUrl = Url.Action("ResetPassword", "Account",
                        new { token = token }, Request.Url.Scheme);

                    var smtpServer = System.Configuration.ConfigurationManager.AppSettings["MerchantHub.SmtpServer"];
                    var smtpPort = int.Parse(System.Configuration.ConfigurationManager.AppSettings["MerchantHub.SmtpPort"]);

                    using (var client = new System.Net.Mail.SmtpClient(smtpServer, smtpPort))
                    {
                        var msg = new System.Net.Mail.MailMessage(
                            "noreply@merchanthub.internal",
                            email,
                            "MerchantHub - Password Reset",
                            string.Format("Click the following link to reset your password: {0}\r\n\r\nThis link expires in 24 hours.", resetUrl));
                        client.Send(msg);
                    }

                    _log.InfoFormat("Password reset requested for: {0}", email);
                }

                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _log.Error("Error in ForgotPassword", ex);
                TempData["Success"] = "If an account with that email exists, a reset link has been sent.";
                return RedirectToAction("Login");
            }
        }

        // GET: /Account/ChangePassword
        [Authorize]
        public ActionResult ChangePassword()
        {
            return View();
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "New password and confirmation do not match.");
                return View();
            }

            if (newPassword.Length < 8)
            {
                ModelState.AddModelError("", "Password must be at least 8 characters.");
                return View();
            }

            try
            {
                if (Membership.Provider.ChangePassword(User.Identity.Name, currentPassword, newPassword))
                {
                    TempData["Success"] = "Password changed successfully.";
                    _log.InfoFormat("Password changed for user: {0}", User.Identity.Name);
                    return RedirectToAction("Dashboard", "Merchant");
                }
                else
                {
                    ModelState.AddModelError("", "Current password is incorrect.");
                    return View();
                }
            }
            catch (Exception ex)
            {
                _log.Error("Error changing password", ex);
                ModelState.AddModelError("", "An error occurred. Please try again.");
                return View();
            }
        }
    }
}
