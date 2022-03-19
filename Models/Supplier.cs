using System.ComponentModel.DataAnnotations;

namespace MinimalApiSample.Models
{
    public class Supplier
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Supplier name is required.")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Supplier document is required.")]
        public string? Document { get; set; }
        public bool IsActive { get; set; }
    }
}