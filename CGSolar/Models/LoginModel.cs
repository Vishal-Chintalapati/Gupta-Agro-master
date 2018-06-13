using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CGSolar.Models
{
    public class LoginModel
    {
        [DisplayName("User Name")]
        public string UserName { get; set; }
        //[Remote("LoginValidation","Home",ErrorMessage ="The User Name or Password entered do not match. Please try again!!")]
        public string Password { get; set; }
    }
}