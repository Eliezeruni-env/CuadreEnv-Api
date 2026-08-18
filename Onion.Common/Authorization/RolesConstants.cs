using System.Collections.Generic;

namespace Onion.Common.Authorization
{
    // Single source of truth for role names and permission->role mappings.
    public static class RolesConstants
    {
        // Role names as stored in DB (do not change these strings)
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string Employee = "Employee";
        public const string SuperAdmin = "SuperAdmin";

        // Permission names used in code. These are string-based and map to roles below.
        // Add new permissions here and update PermissionToRoles map.
        public const string Permission_Company_Read = "Company.Read";
        public const string Permission_Company_Edit = "Company.Edit";
        public const string Permission_Settings_Edit = "Settings.Edit";

        // Map permissions to roles that grant them. This is the canonical mapping.
        public static readonly Dictionary<string, string[]> PermissionToRoles = new()
        {
            { Permission_Company_Read, new[] { SuperAdmin, Admin, Manager } },
            { Permission_Company_Edit, new[] { SuperAdmin, Admin } },
            { Permission_Settings_Edit, new[] { SuperAdmin, Admin } }
        };
    }
}
