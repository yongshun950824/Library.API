using System;

namespace Library.API.Models
{
#pragma warning disable CS1591
    /// <summary>
    /// Model for Author
    /// </summary>
    public class Author
    {        
        /// <summary>
        /// Author's id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Author's first name
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Author's last name
        /// </summary>
        public string LastName { get; set; }
    }
#pragma warning restore CS1591
}
