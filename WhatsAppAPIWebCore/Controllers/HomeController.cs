using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cores;
using InfrastructureCores;
using Microsoft.AspNetCore.Mvc;
using ServiceCores;

namespace WhatsAppAPIWebCore.Controllers
{
    public class HomeController : Controller
    {
        public WAI_DBContext  _context;
        private LoginService _loginService;

        public HomeController()
        {
            _context = new WAI_DBContext();
            _loginService = new LoginService();
        }


        public IActionResult Index()
        {
            return View();
        }

        [Route("Send/{userid?}/{password?}/{division}/{NoHP?}/{Message?}")]
        public async Task<JsonResult> SendMessage(string userid, string password, string division, string NoHP, string Message)
        {
            var isvalid = _loginService.ValidateLogin(userid, password);
            if (isvalid)
            {
                if (string.IsNullOrEmpty(NoHP) || string.IsNullOrEmpty(Message))
                    return Json("Invalid");
                var message = new OutgoingMessage();
                message.receiver = NoHP;
                message.messagetext = Message;
                message.created_date = DateTime.Now;
                message.userid = userid;
                message.divison = division;
                message.sent = false;


                _context.OutgoingMessages.Add(message);
                var res = await _context.SaveChangesAsync();
                return Json("Success");
            }

            return Json("Invalid");

        }
    }
}