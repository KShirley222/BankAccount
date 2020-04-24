using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BankAccounts.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace BankAccounts.Controllers
{
    public class HomeController : Controller
    {
        private MyContext dbContext;
        public HomeController(MyContext context)
        {
            dbContext = context;
        }
        
        [HttpGet("/")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("register")]
        public IActionResult Register(User newUser)
        {
            if(ModelState.IsValid)
            { 
                if(dbContext.Users.Any(u => u.Email == newUser.Email))
                {
                    ModelState.AddModelError("Email", "Email already in use.");
                    return View("index", newUser);
                }

                PasswordHasher<User> Hasher = new PasswordHasher<User>();
                newUser.Password = Hasher.HashPassword( newUser, newUser.Password);
                
                dbContext.Add(newUser);
                dbContext.SaveChanges();

                HttpContext.Session.SetInt32("UserId", newUser.UserId);
                int id = newUser.UserId;

                return Redirect($"/account/{newUser.UserId}");

            }
            return View("index");
        }
        
        [HttpGet("account/{UserId}")]
        public IActionResult Account(User user)
        {
            int? LoginCheck = HttpContext.Session.GetInt32("UserId");
            if(LoginCheck == null)
            {
                return Redirect("/login");
            }
            ViewBag.Total = dbContext.Transactions.Where(u => u.UserId == user.UserId).Sum(x => x.Amount);
            ViewBag.user = dbContext.Users.FirstOrDefault(u => u.UserId == LoginCheck);
            ViewBag.trans = dbContext.Transactions.Where(u => u.UserId == user.UserId).OrderByDescending(l => l.CreatedAt).ToList();
            return View("account");
        }

        [HttpGet("login")]
        public IActionResult LoginScreen()
        {
            return View("login");
        }


        [HttpPost("login")]
        public IActionResult Login(LoginUser userSubmission)
        {
            if(ModelState.IsValid)
            {
                var userInDb = dbContext.Users.FirstOrDefault(u => u.Email == userSubmission.LoginEmail);
                if(userInDb == null)
                {
                    ModelState.AddModelError("Email", "Invalid Email/Password");
                    return View("login");
                }
                else{
                    var hasher = new PasswordHasher<LoginUser>();
                    var result = hasher.VerifyHashedPassword(userSubmission, userInDb.Password, userSubmission.LoginPassword);
                    if(result ==0)
                    {
                        ModelState.AddModelError("Email", "Invalid Email/Password");
                        return View("login");
                    }
                    else{
                        HttpContext.Session.SetInt32("UserId", userInDb.UserId);
                        return Redirect($"/account/{userInDb.UserId}");
                    }
                }
            }
            return View("login");
        }

        [HttpGet("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Redirect("login");
        }

        [HttpPost("/createtransaction")]
        public IActionResult CreateTransaction(Transaction trans)
        {
            int? sesh = HttpContext.Session.GetInt32("UserId");
            User user = dbContext.Users.FirstOrDefault(u => u.UserId == sesh);
            decimal balance = dbContext.Transactions.Where(u => u.UserId == user.UserId).Sum(x => x.Amount);
            decimal amount = trans.Amount;
            Console.WriteLine("+++++++++++++++++++++++++");
            Console.WriteLine(balance);
            Console.WriteLine(amount);
            Console.WriteLine("+++++++++++++++++++++++++");
            if(amount < 0)
            {
                if(Math.Abs(amount) > balance)
                {
                    ModelState.AddModelError("Amount", "You aint got that coin.");
                    return Redirect($"/account/{user.UserId}");
                }
            }  
                dbContext.Transactions.Add(trans);
                dbContext.SaveChanges();
                return Redirect($"/account/{user.UserId}");
        }
    }
}
