using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (context.Database.IsSqlite())
            await context.Database.EnsureCreatedAsync();
        else
            await context.Database.MigrateAsync();

        await EnsureExtraTablesAsync(context);

        string[] roles = { "Admin", "Doctor", "Receptionist", "Patient" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        await CreateUser(userManager, "admin@clinic.com", "Admin123", "System Administrator", "Admin");
        var doctorUser = await CreateUser(userManager, "doctor@clinic.com", "Doctor123", "Dr. Ahmed Hassan", "Doctor");
        await CreateUser(userManager, "receptionist@clinic.com", "Reception123", "Sara Receptionist", "Receptionist");
        var patientUser = await CreateUser(userManager, "patient@clinic.com", "Patient123", "Mohamed Patient", "Patient");

        if (!await context.Doctors.AnyAsync())
        {
            context.Doctors.Add(new Doctor
            {
                FullName = "Dr. Ahmed Hassan",
                Specialty = "General Medicine",
                Phone = "01000000000",
                Email = "doctor@clinic.com",
                ApplicationUserId = doctorUser.Id
            });
        }

        if (!await context.Patients.AnyAsync())
        {
            context.Patients.Add(new Patient
            {
                FullName = "Mohamed Patient",
                DateOfBirth = new DateTime(2002, 1, 1),
                Gender = "Male",
                Phone = "01111111111",
                Address = "Egypt",
                Email = "patient@clinic.com",
                ApplicationUserId = patientUser.Id
            });
        }

        if (!await context.Receptionists.AnyAsync())
        {
            context.Receptionists.Add(new Receptionist
            {
                FullName = "Sara Receptionist",
                Phone = "01222222222",
                Email = "receptionist@clinic.com"
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureExtraTablesAsync(ApplicationDbContext context)
    {
        var createSqliteTables = """
            CREATE TABLE IF NOT EXISTS "PrescriptionItems" (
                "PrescriptionItemId" INTEGER NOT NULL CONSTRAINT "PK_PrescriptionItems" PRIMARY KEY AUTOINCREMENT,
                "PrescriptionId" INTEGER NOT NULL,
                "MedicineName" TEXT NOT NULL,
                "Dosage" TEXT NOT NULL,
                "Frequency" TEXT NOT NULL,
                "Duration" TEXT NOT NULL,
                "Instructions" TEXT NULL,
                CONSTRAINT "FK_PrescriptionItems_Prescriptions_PrescriptionId"
                    FOREIGN KEY ("PrescriptionId") REFERENCES "Prescriptions" ("PrescriptionId") ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS "IX_PrescriptionItems_PrescriptionId"
                ON "PrescriptionItems" ("PrescriptionId");
            CREATE TABLE IF NOT EXISTS "BillItems" (
                "BillItemId" INTEGER NOT NULL CONSTRAINT "PK_BillItems" PRIMARY KEY AUTOINCREMENT,
                "BillId" INTEGER NOT NULL,
                "Description" TEXT NOT NULL,
                "Quantity" NUMERIC NOT NULL,
                "UnitPrice" NUMERIC NOT NULL,
                CONSTRAINT "FK_BillItems_Bills_BillId"
                    FOREIGN KEY ("BillId") REFERENCES "Bills" ("BillId") ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS "IX_BillItems_BillId" ON "BillItems" ("BillId");
            CREATE TABLE IF NOT EXISTS "Payments" (
                "PaymentId" INTEGER NOT NULL CONSTRAINT "PK_Payments" PRIMARY KEY AUTOINCREMENT,
                "BillId" INTEGER NOT NULL,
                "Amount" NUMERIC NOT NULL,
                "Method" TEXT NOT NULL,
                "PaymentDate" TEXT NOT NULL,
                "RecordedByUserId" TEXT NULL,
                CONSTRAINT "FK_Payments_Bills_BillId"
                    FOREIGN KEY ("BillId") REFERENCES "Bills" ("BillId") ON DELETE CASCADE,
                CONSTRAINT "FK_Payments_AspNetUsers_RecordedByUserId"
                    FOREIGN KEY ("RecordedByUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE SET NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_Payments_BillId" ON "Payments" ("BillId");
            CREATE INDEX IF NOT EXISTS "IX_Payments_RecordedByUserId" ON "Payments" ("RecordedByUserId");
            UPDATE "Bills" SET "PaymentStatus" = 'Unpaid' WHERE "PaymentStatus" = 'Pending';
            INSERT INTO "BillItems" ("BillId", "Description", "Quantity", "UnitPrice")
                SELECT bill."BillId", COALESCE(NULLIF(bill."Description", ''), 'Legacy invoice item'), 1, bill."Amount"
                FROM "Bills" AS bill
                WHERE NOT EXISTS (SELECT 1 FROM "BillItems" AS item WHERE item."BillId" = bill."BillId");
            INSERT INTO "Payments" ("BillId", "Amount", "Method", "PaymentDate", "RecordedByUserId")
                SELECT bill."BillId", bill."Amount", 'Legacy', COALESCE(bill."PaidDate", bill."BillDate"), NULL
                FROM "Bills" AS bill
                WHERE bill."PaymentStatus" = 'Paid'
                  AND NOT EXISTS (SELECT 1 FROM "Payments" AS payment WHERE payment."BillId" = bill."BillId");
            """;

        var createSqlServerTables = """
            IF OBJECT_ID(N'dbo.PrescriptionItems', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[PrescriptionItems] (
                    [PrescriptionItemId] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_PrescriptionItems] PRIMARY KEY,
                    [PrescriptionId] int NOT NULL,
                    [MedicineName] nvarchar(max) NOT NULL,
                    [Dosage] nvarchar(max) NOT NULL,
                    [Frequency] nvarchar(max) NOT NULL,
                    [Duration] nvarchar(max) NOT NULL,
                    [Instructions] nvarchar(max) NULL,
                    CONSTRAINT [FK_PrescriptionItems_Prescriptions_PrescriptionId]
                        FOREIGN KEY ([PrescriptionId]) REFERENCES [dbo].[Prescriptions] ([PrescriptionId]) ON DELETE CASCADE
                );
                CREATE INDEX [IX_PrescriptionItems_PrescriptionId]
                    ON [dbo].[PrescriptionItems] ([PrescriptionId]);
            END;
            IF OBJECT_ID(N'dbo.BillItems', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[BillItems] (
                    [BillItemId] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_BillItems] PRIMARY KEY,
                    [BillId] int NOT NULL,
                    [Description] nvarchar(max) NOT NULL,
                    [Quantity] decimal(10,2) NOT NULL,
                    [UnitPrice] decimal(10,2) NOT NULL,
                    CONSTRAINT [FK_BillItems_Bills_BillId] FOREIGN KEY ([BillId]) REFERENCES [dbo].[Bills] ([BillId]) ON DELETE CASCADE
                );
                CREATE INDEX [IX_BillItems_BillId] ON [dbo].[BillItems] ([BillId]);
            END;
            IF OBJECT_ID(N'dbo.Payments', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Payments] (
                    [PaymentId] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Payments] PRIMARY KEY,
                    [BillId] int NOT NULL,
                    [Amount] decimal(10,2) NOT NULL,
                    [Method] nvarchar(max) NOT NULL,
                    [PaymentDate] datetime2 NOT NULL,
                    [RecordedByUserId] nvarchar(450) NULL,
                    CONSTRAINT [FK_Payments_Bills_BillId] FOREIGN KEY ([BillId]) REFERENCES [dbo].[Bills] ([BillId]) ON DELETE CASCADE,
                    CONSTRAINT [FK_Payments_AspNetUsers_RecordedByUserId] FOREIGN KEY ([RecordedByUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE SET NULL
                );
                CREATE INDEX [IX_Payments_BillId] ON [dbo].[Payments] ([BillId]);
                CREATE INDEX [IX_Payments_RecordedByUserId] ON [dbo].[Payments] ([RecordedByUserId]);
            END;
            IF COL_LENGTH(N'dbo.Bills', N'DueDate') IS NULL
                ALTER TABLE [dbo].[Bills] ADD [DueDate] datetime2 NULL;
            UPDATE [dbo].[Bills] SET [PaymentStatus] = N'Unpaid' WHERE [PaymentStatus] = N'Pending';
            INSERT INTO [dbo].[BillItems] ([BillId], [Description], [Quantity], [UnitPrice])
                SELECT bill.[BillId], COALESCE(NULLIF(bill.[Description], N''), N'Legacy invoice item'), 1, bill.[Amount]
                FROM [dbo].[Bills] AS bill
                WHERE NOT EXISTS (SELECT 1 FROM [dbo].[BillItems] AS item WHERE item.[BillId] = bill.[BillId]);
            INSERT INTO [dbo].[Payments] ([BillId], [Amount], [Method], [PaymentDate], [RecordedByUserId])
                SELECT bill.[BillId], bill.[Amount], N'Legacy', COALESCE(bill.[PaidDate], bill.[BillDate]), NULL
                FROM [dbo].[Bills] AS bill
                WHERE bill.[PaymentStatus] = N'Paid'
                  AND NOT EXISTS (SELECT 1 FROM [dbo].[Payments] AS payment WHERE payment.[BillId] = bill.[BillId]);
            """;

        if (context.Database.IsSqlite())
        {
            await context.Database.ExecuteSqlRawAsync(createSqliteTables);
            await EnsureSqliteColumnAsync(context, "Bills", "DueDate", "TEXT NULL");
        }
        else
        {
            await context.Database.ExecuteSqlRawAsync(createSqlServerTables);
        }
    }

    private static async Task EnsureSqliteColumnAsync(
        ApplicationDbContext context,
        string tableName,
        string columnName,
        string columnDefinition)
    {
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        try
        {
            await using var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = $"PRAGMA table_info(\"{tableName}\");";
            await using var reader = await checkCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            await reader.CloseAsync();
            await using var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{columnName}\" {columnDefinition};";
            await alterCommand.ExecuteNonQueryAsync();
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    private static async Task<ApplicationUser> CreateUser(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string fullName,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        if (!await userManager.IsInRoleAsync(user, role))
            await userManager.AddToRoleAsync(user, role);

        return user;
    }
}