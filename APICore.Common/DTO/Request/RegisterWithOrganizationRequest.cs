using System;
using System.ComponentModel.DataAnnotations;

namespace APICore.Common.DTO.Request
{

    public class RegisterWithOrganizationRequest
    {
       
        [Required]
        public string OrganizationName { get; set; }

        [Required]
        public string OrganizationCode { get; set; }

        public string OrganizationDescription { get; set; }

     
        [Required]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmationPassword { get; set; }

        [Required]
        public DateTime Birthday { get; set; }

        public string Phone { get; set; }
    }
}
