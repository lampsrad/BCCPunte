using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BCC.Viewmodels
{
  public class Pdf
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Please enter your Name")]
        public string Name { get; set; }
        [Required(AllowEmptyStrings = false, ErrorMessage = "Please enter your Email address")]
        public string Email { get; set; }
        [Required(AllowEmptyStrings = false, ErrorMessage = "Please enter Mobile number")]
        public string Mobile { get; set; }
    }
}
