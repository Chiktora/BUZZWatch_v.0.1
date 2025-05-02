using BuzzWatch.Contracts.Alerts;
using BuzzWatch.Contracts.Devices;

namespace BuzzWatch.Web.Models
{
    public class AlertsViewModel
    {
        public List<AlertDto> Alerts { get; set; } = new List<AlertDto>();
        public List<DeviceDto> Devices { get; set; } = new List<DeviceDto>();
        
        public string SelectedSeverity { get; set; }
        public string SelectedStatus { get; set; }
        
        public int TotalAlerts => Alerts.Count;
        public int ActiveAlerts => Alerts.Count(a => a.Status == "Active");
        public int AcknowledgedAlerts => Alerts.Count(a => a.Status == "Acknowledged");
        public int ResolvedAlerts => Alerts.Count(a => a.Status == "Resolved");
        
        public int HighSeverityAlerts => Alerts.Count(a => a.Severity == "High");
        public int MediumSeverityAlerts => Alerts.Count(a => a.Severity == "Medium");
        public int LowSeverityAlerts => Alerts.Count(a => a.Severity == "Low");
        
        public string GetDeviceName(Guid deviceId)
        {
            return Devices.FirstOrDefault(d => d.Id == deviceId)?.Name ?? "Unknown Device";
        }
        
        public List<SelectListItem> StatusFilterOptions => new List<SelectListItem>
        {
            new SelectListItem { Value = "", Text = "All Statuses" },
            new SelectListItem { Value = "Active", Text = "Active" },
            new SelectListItem { Value = "Acknowledged", Text = "Acknowledged" },
            new SelectListItem { Value = "Resolved", Text = "Resolved" }
        };
        
        public List<SelectListItem> SeverityFilterOptions => new List<SelectListItem>
        {
            new SelectListItem { Value = "", Text = "All Severities" },
            new SelectListItem { Value = "High", Text = "High" },
            new SelectListItem { Value = "Medium", Text = "Medium" },
            new SelectListItem { Value = "Low", Text = "Low" }
        };
    }
    
    public class SelectListItem
    {
        public string Value { get; set; }
        public string Text { get; set; }
        public bool Selected { get; set; }
    }
} 