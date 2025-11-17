using System;
using System.ComponentModel.DataAnnotations;
namespace FleetManager.Models
{
    public class Driver
    {
        public int id {get;set;}
        [Required]
        public string FirstName{get;set;} =string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName{get;set;} =string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email{get;set;}=string.Empty;

        [Phone]
        [StringLength(20)]
        public string? Phone {get;set;}

        [Required]
        [StringLength(30)]
        public string LicenseNumber {get;set;} = string.Empty;

        public DateTime HireDate {get;set;} = DateTime.Now; 
    }
}