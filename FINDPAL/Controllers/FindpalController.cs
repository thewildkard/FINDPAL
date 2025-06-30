using FINDPAL.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FINDPAL.Controllers
{
    public class FindpalController : Controller
    {
        private FINDPALEntities3 dbfind = new FINDPALEntities3();

        // GET: Login
        public ActionResult Login()
        {
            return View();
        }

        // POST: Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(User user)
        {
            if (!ModelState.IsValid)
            {
                return View(user);
            }

            var foundUser = dbfind.Users.FirstOrDefault(u => u.UserName == user.UserName && u.Hpassword == user.Hpassword);
            if (foundUser != null)
            {
                Session["CurrentUserId"] = foundUser.UserID;
                Session["CurrentUserName"] = foundUser.UserName;
                Session["CurrentUserRole"] = foundUser.Role; // Add this line
                return RedirectToAction("Dashboard");
            }
            if (Session["CurrentUserRole"] == null || Session["CurrentUserRole"].ToString() != "Admin")
                return RedirectToAction("Dashboard");


            ModelState.AddModelError("", "Invalid login attempt.");
            return View(user);
        }

        // GET: SignUp
        public ActionResult SignUp()
        {
            return View();
        }

        // POST: SignUp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SignUp(User user)
        {
            if (ModelState.IsValid)
            {
                // Ensure username is unique
                if (dbfind.Users.Any(u => u.UserName == user.UserName))
                {
                    ModelState.AddModelError("UserName", "Username already exists.");
                    return View(user);
                }

                // Ensure RollNumber is unique
                if (dbfind.Users.Any(u => u.RollNumber == user.RollNumber))
                {
                    ModelState.AddModelError("RollNumber", "Roll Number already exists.");
                    return View(user);
                }

                dbfind.Users.Add(user);
                dbfind.SaveChanges();

                TempData["SignupSuccess"] = "Account created successfully. Your Reference Number is: " + user.RollNumber;
                return RedirectToAction("Login");
            }

            return View(user);
        }

        // GET: Dashboard
        public ActionResult Dashboard()
        {
            ViewBag.CurrentUserName = Session["CurrentUserName"] as string;
            int? userId = Session["CurrentUserId"] as int?;
            if (userId == null)
                return RedirectToAction("Login");

            var foundItems = dbfind.Items
                .Where(i => i.Status == "Found" && i.ReporterUserId != userId)
                .OrderByDescending(i => i.DateReported)
                .Take(5)
                .ToList();

            var myReports = dbfind.Items
                .Where(i => i.ReporterUserId == userId)
                .OrderByDescending(i => i.DateReported)
                .ToList();

            var notifications = dbfind.ScanNotifications
                .Where(sn => sn.FinderUserId == userId && !sn.NotificationSent)
                .ToList();

            ViewBag.FoundItems = foundItems;
            ViewBag.MyReports = myReports;
            ViewBag.Notifications = notifications;
            ViewBag.TotalReports = myReports.Count;
            ViewBag.NewNotifications = notifications.Count;

            return View();
        }

        // GET: Report
        public ActionResult Report()
        {
            ViewData["Categories"] = new SelectList(dbfind.PostCategories, "PostCategoryId", "Name");
            ViewData["PostTypes"] = new SelectList(dbfind.PostTypes, "PostTypeId", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Report(Report model)
        {
            if (ModelState.IsValid)
            {
                // Set additional fields if needed
                model.CreatedAt = DateTime.Now;
                // Optionally set UserId if you want to track the user
                int? userId = Session["CurrentUserId"] as int?;
                if (userId != null)
                    model.UserId = userId;

                dbfind.Reports.Add(model);
                dbfind.SaveChanges();

                ViewBag.Success = "Report submitted successfully!";
                ModelState.Clear();
                // Optionally redirect to a success page
                return RedirectToAction("Success");
            }

            // Repopulate dropdowns if validation fails
            ViewData["Categories"] = new SelectList(dbfind.PostCategories, "PostCategoryId", "Name");
            ViewData["PostTypes"] = new SelectList(dbfind.PostTypes, "PostTypeId", "Name");
            ViewBag.Error = "Please correct the errors and try again.";
            return View(model);
        }


        // GET: Success
        public ActionResult Success()
        {
            int? userId = Session["CurrentUserId"] as int?;
            if (userId == null)
                return RedirectToAction("Login");
            return View();
        }

        // GET: LostAndFound
        public ActionResult LostAndFound()
        {
            int? userId = Session["CurrentUserId"] as int?;
            if (userId == null)
                return RedirectToAction("Login");

            bool isAdmin = (Session["CurrentUserName"] != null && Session["CurrentUserName"].ToString().ToLower() == "admin");

            List<Report> reports;
            if (isAdmin)
            {
                reports = dbfind.Reports.Include("User").Include("PostCategory").Include("PostType")
                    .OrderByDescending(r => r.Date)
                    .ToList();
                ViewBag.IsAdmin = true;
            }
            else
            {
                reports = dbfind.Reports.Include("User").Include("PostCategory").Include("PostType")
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.Date)
                    .ToList();
                ViewBag.IsAdmin = false;
            }

            return View(reports);
        }




        // GET: Notifications
        public ActionResult Notifications()
        {
            int? userId = Session["CurrentUserId"] as int?;
            if (userId == null)
                return RedirectToAction("Login");

            var notifications = dbfind.ScanNotifications
                .Where(sn => sn.FinderUserId == userId && !sn.NotificationSent)
                .ToList();

            return View(notifications);
        }


        // POST: Mark Notification as Sent
        [HttpPost]
        public ActionResult MarkNotificationAsSent(int notificationId)
        {
            var notification = dbfind.ScanNotifications.FirstOrDefault(n => n.NotificationId == notificationId);
            if (notification != null)
            {
                notification.NotificationSent = true;
                dbfind.SaveChanges();
            }
            return RedirectToAction("Notifications");
        }

       


        // GET: Admin Page
        //[Authorize(Roles = "Admin")]
        public ActionResult AdminPage()
        {
            var users = dbfind.Users.ToList();
            var items = dbfind.Items.ToList();
            ViewBag.Users = users;
            ViewBag.Items = items;
            return View();
        }

        // POST: Delete Item (Admin Only)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteItem(int itemId)
        {
            var item = dbfind.Items.FirstOrDefault(i => i.ItemId == itemId);
            if (item != null)
            {
                dbfind.Items.Remove(item);
                dbfind.SaveChanges();
            }
            return RedirectToAction("AdminPage");
        }

        // GET: UserManagement
        public ActionResult UserManagement()
        {
            // Check if the current user is admin (customize this check as per your role management)
            bool isAdmin = (Session["CurrentUserName"] != null && Session["CurrentUserName"].ToString().ToLower() == "admin");

            if (isAdmin)
            {
                // Admin sees all users
                var users = dbfind.Users.ToList();
                ViewBag.IsAdmin = true;
                return View(users);
            }
            else
            {
                // Normal user sees only their own info
                int? userId = Session["CurrentUserId"] as int?;
                if (userId == null)
                    return RedirectToAction("Login");

                var user = dbfind.Users.FirstOrDefault(u => u.UserID == userId.Value);
                if (user == null)
                    return RedirectToAction("Login"); // or show an error view

                ViewBag.IsAdmin = false;
                return View(new List<User> { user });
            }
        }

        // GET: Edit User
        [HttpGet]
        public ActionResult EditUser(int? id)
        {
            if (id == null)
                return RedirectToAction("UserManagement");

            var user = dbfind.Users.FirstOrDefault(u => u.UserID == id.Value);
            if (user == null)
                return HttpNotFound();
            return View(user);
        }

        // POST: Edit User
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditUser(User model)
        {
            if (ModelState.IsValid)
            {
                var user = dbfind.Users.FirstOrDefault(u => u.UserID == model.UserID);
                if (user != null)
                {
                    user.UserName = model.UserName;
                    user.RollNumber = model.RollNumber;
                    user.Hpassword = model.Hpassword;
                    dbfind.SaveChanges();
                }
                return RedirectToAction("UserManagement");
            }
            return View(model);
        }

        // GET: Add User
        [HttpGet]
        public ActionResult AddUser()
        {
            return View();
        }

        // POST: Add User
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddUser(User model)
        {
            if (ModelState.IsValid)
            {
                dbfind.Users.Add(model);
                dbfind.SaveChanges();
                return RedirectToAction("UserManagement");
            }
            return View(model);
        }

        // POST: Delete User
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteUser(int id)
        {
            var user = dbfind.Users.FirstOrDefault(u => u.UserID == id);
            if (user != null)
            {
                dbfind.Users.Remove(user);
                dbfind.SaveChanges();
            }
            return RedirectToAction("UserManagement");
        }


    }
}
