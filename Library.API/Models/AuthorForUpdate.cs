using System.ComponentModel.DataAnnotations;

namespace Library.API.Models
{
    /// <summary>
    /// Model for update Author action
    /// </summary>
    public class AuthorForUpdate
    {

        /// <summary>
        /// Author's first name
        /// </summary>
        [Required]
        [MaxLength(150)] 
        public string FirstName { get; set; }

        /// <summary>
        /// Author's last name
        /// </summary>
        [Required]
        [MaxLength(150)]
        public string LastName { get; set; }
    }
}
