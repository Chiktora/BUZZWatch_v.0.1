using BuzzWatch.Contracts.Devices;

namespace BuzzWatch.Web.Models
{
    public class DashboardViewModel
    {
        public List<DeviceDto> Devices { get; set; } = new List<DeviceDto>();
    }
} 