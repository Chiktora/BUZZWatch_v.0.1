# BuzzWatch - Beehive Monitoring System

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