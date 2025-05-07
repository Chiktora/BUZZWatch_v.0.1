# BuzzWatch - Hive Monitoring System

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4) ![License](https://img.shields.io/badge/license-MIT-green) ![Platform](https://img.shields.io/badge/platform-ASP.NET%20Core-blue)

## Overview
BuzzWatch is a comprehensive hive monitoring solution designed to help beekeepers monitor and maintain the health of their beehives. The system collects, analyzes, and visualizes environmental data from connected devices, providing insights into hive conditions and enabling timely interventions.

## Features

### Monitoring
- Real-time monitoring of beehive environmental conditions:
  - Internal temperature and humidity
  - External temperature and humidity
  - Hive weight
- Customizable alerts and notifications
- Historical data tracking and trend analysis

### Management
- Multi-user access control with role-based permissions:
  - Admin: Full system access and configuration
  - User: View assigned devices and data
  - ReadOnly: Limited view-only access to specific data
- Device management and configuration
- User management with fine-grained permissions

### Analytics
- Data visualization dashboards
- Comparative analysis between hives
- Predictive insights for potential issues
- Export capabilities for further analysis

## Technical Architecture

### Solution Structure
- **BuzzWatch.Api**: Backend API service providing data endpoints and business logic
- **BuzzWatch.Web**: Frontend web application for user interface
- **BuzzWatch.Infrastructure**: Data access, external service integrations, and infrastructure concerns
- **BuzzWatch.Domain**: Core business entities and logic
- **BuzzWatch.Contracts**: Shared data transfer objects and service contracts
- **BuzzWatch.Application**: Application services and business operations

### Technologies
- **.NET 8**: Modern cross-platform framework for backend services
- **ASP.NET Core MVC**: Web framework for the user interface
- **Entity Framework Core**: ORM for database access and migrations
- **SQL Server**: Relational database for data storage
- **Bootstrap 5**: Frontend framework for responsive design
- **SignalR**: Real-time communication for live updates
- **JWT Authentication**: Secure token-based authentication

## Setup and Installation

### Prerequisites
- .NET 8 SDK
- SQL Server (2019 or later)
- Visual Studio 2022 or later (recommended) or Visual Studio Code

### Database Setup
1. Update the connection string in `appsettings.json` in both the API and Web projects
2. Run database migrations:
   ```
   dotnet ef database update --project BuzzWatch.Infrastructure --startup-project BuzzWatch.Api
   ```

### Running the Application
1. Start the API server:
   ```
   cd BuzzWatch.Api
   dotnet run
   ```
2. In a separate terminal, start the web application:
   ```
   cd BuzzWatch.Web
   dotnet run
   ```
3. Access the web interface at https://localhost:7195
4. Login with default admin credentials:
   - Email: admin@local
   - Password: Admin123!

## Common Issues and Troubleshooting

### Device Assignment
If you encounter issues with device assignment showing errors like "Error updating device permissions", ensure that:
1. The database migrations have been properly applied
2. The user has the correct permissions
3. The device exists and is available for assignment

### API Connection Issues
If the web application cannot connect to the API:
1. Verify both applications are running
2. Check the API URL configuration in the web application
3. Ensure the API server is listening on the expected port
4. Verify JWT tokens are being properly generated and validated

### Database Migration Issues
If you encounter database-related errors:
1. Verify your SQL Server instance is running
2. Check connection strings in both projects
3. Ensure all migrations have been applied:
   ```
   dotnet ef migrations list --project BuzzWatch.Infrastructure
   ```

## User Guide

### Admin Dashboard
The admin dashboard provides an overview of all system metrics and allows configuration of:
- Users and permissions
- Device settings
- Alert thresholds
- System settings

### Device Management
Administrators can:
- Add new devices
- Configure device settings
- Assign devices to users
- View device status and health

### User Management
Administrators can:
- Create new user accounts
- Assign roles and permissions
- Grant device access to specific users
- Disable/enable user accounts

### Data Visualization
All users can view visualizations of:
- Current hive conditions
- Historical trends
- Comparative analysis between hives
- Predictive insights

## License
[Specify license information here]

## Contributing
[Guidelines for contributing to the project]

## Implementation Summary

### Step 5 - Security & Identity

#### 5.1 ASP.NET Identity Integration
- Added Microsoft.AspNetCore.Identity.EntityFrameworkCore to the Infrastructure project
- Created an AppUser class inheriting from IdentityUser<Guid>
- Updated ApplicationDbContext to inherit from IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
- Added migration for Identity tables

#### 5.2 JWT Bearer Authentication
- Added Microsoft.AspNetCore.Authentication.JwtBearer package
- Implemented JwtGenerator utility for creating tokens
- Added "/api/v1/auth/login" endpoint that returns JWT tokens
- Configured JWT token validation in Program.cs
- Added JWT configuration in appsettings.json

#### 5.3 API Key Authentication for IoT Devices
- Created ApiKey domain entity with Issue() factory method
- Implemented ApiKeyAuthenticationHandler to validate API keys from X-Api-Key header
- Applied [Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.Scheme)] to measurement endpoints

#### 5.4 Authorization Policies
- Implemented "OwnsDevice" policy with authorization handler
- Created logic to check if a user has access to a specific device
- Configured roles (Admin, Moderator, User)

#### 5.5 Identity Seeding
- Created SeedIdentityAsync extension method to create roles and admin user
- Admin user created with credentials: admin@local / Pa$$w0rd!
- Added calls in Program.cs to seed data in development environment

#### 5.6 Integration Tests
- Created tests for JWT login and API key authentication
- Verified unauthorized access is properly handled
- Tested device access with proper authorization

### Current Status

- ✅ Identity is integrated and migrations created
- ✅ JWT authentication works for human users
- ✅ API key authentication scheme for IoT devices
- ✅ Authorization policies implemented 
- ✅ Admin user & roles seeding works
- ✅ Basic integration tests for authentication flow 

## User Experience Enhancements

### Dark Mode & Theming
- Implemented a comprehensive dark mode with theme variables
- Added system preference detection for automatic theme switching
- Created persistent theme storage using localStorage
- Added a theme toggle button in the UI

### Mobile-Responsive Design
- Enhanced all views with responsive design patterns
- Optimized dashboard layouts for mobile devices
- Implemented horizontal scrolling tabs for small screens
- Created responsive navigation and menu components
- Used Bootstrap's responsive grid with custom breakpoints

### User Preferences & Customization
- Created a dedicated user preferences page with multiple sections:
  - Appearance settings (light/dark/system themes)
  - Dashboard customization (layout, widgets, time ranges)
  - Notification preferences and email settings
  - Data visualization preferences (units, graph types)
- Implemented client-side preference storage
- Added server-side preference endpoints
- Built a preference reset functionality

### Dashboard Improvements
- Redesigned the dashboard with a modern widget-based approach
- Implemented customizable layouts (grid/list/compact)
- Added widget visibility toggles and ordering
- Enhanced dashboard metrics and visualizations
- Integrated real-time updates using SignalR

### Bug Fixes & Optimizations
- Fixed compilation errors related to missing dependencies (CsvHelper)
- Improved async/await patterns in controllers
- Fixed CSS media query syntax in Razor views
- Optimized data loading patterns
- Enhanced error handling and validation

## Data Export Functionality

### Comprehensive Data Export System
- Implemented export to multiple formats:
  - CSV format using CsvHelper
  - JSON format with proper formatting and metadata
  - Excel format with ClosedXML including styled headers and multiple sheets
- Added data exports from multiple views:
  - Device details page with configurable time ranges
  - Analytics views with export capabilities
  - Dashboard with device-specific exports
- Enhanced exports with metadata and statistics:
  - Device information included in exports
  - Timestamp and export information
  - Analytics sheet with summary statistics in Excel exports

### Export Features
- User-configurable export options:
  - Selectable date ranges (7 days, 30 days, 90 days, 1 year)
  - Multiple format options (CSV, JSON, Excel)
  - One-click export triggering
- Optimized for large datasets:
  - Efficient memory handling with MemoryStream
  - Proper response headers for browser downloads
  - Descriptive filenames with timestamps

## Next Steps

- Enhance predictive analytics with more algorithms
- Add internationalization support
- Create mobile app version 