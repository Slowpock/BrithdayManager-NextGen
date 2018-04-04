﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Http.Results;
using System.Web.Mvc;
using AutoMapper;
using BirthdayManager.Controllers.Api;
using BirthdayManager.Core.Constants;
using BirthdayManager.Core.Enums;
using BirthdayManager.Core.Models;
using BirthdayManager.Core.ViewModels;
using BirthdayManager.Persistence;

namespace BirthdayManager.Controllers
{
    public class UsersController : Controller
    {
        private ApplicationDbContext _context;

        public UsersController()
        {
            _context = new ApplicationDbContext();
        }

        [HttpGet]
        [Route("Users")]
        public ActionResult List()
        {
            var users = _context.Users.ToList();

            if (User.IsInRole(RoleNames.Admin))
                return View("List", users);

            return View("ReadOnlyList", users);
        }

        [HttpGet]
        public ActionResult Details(string username)
        {
            var user = _context.Users.SingleOrDefault(x => x.UserName.ToLower() == username.ToLower());

            if (user == null)
                return HttpNotFound();

            if (User.IsInRole(RoleNames.Admin))
            {              
                var subscriptions = _context.Subscriptions
                    .Include(x => x.User)
                    .Include(x => x.Arrangement.ApplicationUser)
                    .Where(x => x.ApplicationUserId == user.Id && !x.Arrangement.IsComplete)                   
                    .ToList();

                var payments = _context.MoneyTransactions.Where(x => x.ApplicationUserId == user.Id && x.Type == TransactionType.Supply)
                    .Include(x => x.ApplicationUser)
                    .OrderByDescending(x => x.Date)
                    .Take(10)
                    .ToList();

                var viewModel = new DetailsAdminViewModel()
                {
                    Subscriptions = subscriptions,
                    Payments = payments,
                    User = user
                };

                return View("DetailsAdmin", viewModel);
            }

            return View(user);
        }

        [Authorize(Roles = RoleNames.Admin)]
        public ActionResult Edit(string username)
        {
            var user = _context.Users.SingleOrDefault(x => x.UserName == username);

            if (user == null)
            {
                return HttpNotFound();
            }

            var userViewModel = Mapper.Map<ApplicationUser, UserFormViewModel>(user);

            return View("UserForm", userViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Admin)]
        public ActionResult Save(UserFormViewModel user)
        {
            if (!ModelState.IsValid)
            {
                return View("UserForm", user);
            }

            if (string.IsNullOrEmpty(user.Id))
            {
                //TODO: implement add user when id  is empty
               throw new Exception("User Id should be provided");
            }

            var customerRetrieved = _context.Users.Single(c => c.Id == user.Id);
            customerRetrieved.UserName = user.UserName;
            customerRetrieved.Email = user.Email;
            customerRetrieved.DayOfBirth = user.DayOfBirth;
            customerRetrieved.MonthOfBirth = user.MonthOfBirth;
            customerRetrieved.LastName = user.LastName;
            customerRetrieved.FirstName = user.FirstName;
            customerRetrieved.Location = user.Location;
            customerRetrieved.Balance = user.Balance;

            _context.SaveChanges();

            return RedirectToAction("List", "Users");
        }


        protected override void Dispose(bool disposing)
        {
            _context.Dispose();
        }
    }
}