using Infrastructure;
using Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace WhatsappWebMSI.Controllers
{
    public class HomeController : Controller
    {
        public WAModel _context;
        private LoginService _loginService;
        public HomeController()
        {
            _context = new WAModel();
            _loginService = new LoginService();
        }
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        [Route("Send/{userid?}/{password?}/{division}/{NoHP?}/{Message?}")]
        public async Task<JsonResult> SendMessage(string userid,string password,string division,string NoHP,string Message)
        {
            var isvalid = _loginService.ValidateLogin(userid, password);
            if (isvalid)
            {
                if (string.IsNullOrEmpty(NoHP) || string.IsNullOrEmpty(Message))
                    return Json("Invalid", JsonRequestBehavior.AllowGet);
                var message = new OutgoingMessage();
                message.receiver = NoHP;
                message.messagetext = Message;
                message.created_date = DateTime.Now;
                message.userid = userid;
                message.divison = division;
                message.sent = false;


                _context.OutgoingMessages.Add(message);
                var res = await _context.SaveChangesAsync();
                return  Json("Success", JsonRequestBehavior.AllowGet);
            }

            return Json("Invalid", JsonRequestBehavior.AllowGet);

        }
    }
}