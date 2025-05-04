using System.ComponentModel.DataAnnotations;

namespace BuzzWatch.Contracts.Devices
{
    /// <summary>
    /// Request model for creating a new device
    /// </summary>
    public class CreateDeviceRequest
    {
        /// <summary>
        /// The name of the device
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// The location description (e.g. "Apiary 1", "North Field")
        /// </summary>
        public string? Location { get; set; }
        
        /// <summary>
        /// Optional latitude coordinate of the device location
        /// </summary>
        [Range(-90, 90)]
        public double? Latitude { get; set; }
        
        /// <summary>
        /// Optional longitude coordinate of the device location
        /// </summary>
        [Range(-180, 180)]
        public double? Longitude { get; set; }
    }
} 