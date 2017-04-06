using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Bundler.Web2.Controllers {
    public class HomeController : Controller {

        public ActionResult Index() {
            return RedirectToAction(nameof(Bootstrap3));
        }

        public ActionResult Bootstrap3() {
            return View();
        }

        public ActionResult Bootstrap4() {
            return View();
        }
    }
}