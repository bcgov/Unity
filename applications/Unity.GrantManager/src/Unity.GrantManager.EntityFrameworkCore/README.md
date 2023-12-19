#Host and Tenant migrations:

dotnet ef migrations add {name} --context GrantManagerDbContext --output-dir Migrations/HostMigrations

dotnet ef migrations add {name} --context GrantTenantDbContext --output-dir Migrations/TenantMigrations