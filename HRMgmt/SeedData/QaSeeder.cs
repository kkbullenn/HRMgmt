using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HRMgmt.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMgmt.SeedData;

internal static class QaSeeder
{
    private static readonly List<Guid> TestUserIds = new()
    {
        Guid.Parse("11111111-1111-1111-1111-000000000001"),
        Guid.Parse("11111111-1111-1111-1111-000000000002"),
        Guid.Parse("11111111-1111-1111-1111-000000000003"),
        Guid.Parse("11111111-1111-1111-1111-000000000005"),
        Guid.Parse("11111111-1111-1111-1111-000000000006"),
        Guid.Parse("11111111-1111-1111-1111-000000000007"),
        Guid.Parse("11111111-1111-1111-1111-000000000008")
    };

    private static readonly List<Guid> TestShiftIds = new()
    {
        Guid.Parse("22222222-2222-2222-2222-000000000001"),
        Guid.Parse("22222222-2222-2222-2222-000000000002"),
        Guid.Parse("22222222-2222-2222-2222-000000000004"),
        Guid.Parse("22222222-2222-2222-2222-000000000005"),
        Guid.Parse("22222222-2222-2222-2222-000000000006"),
        Guid.Parse("22222222-2222-2222-2222-000000000007"),
        Guid.Parse("22222222-2222-2222-2222-000000000008")
    };

    public static void Seed(OrgDbContext db, string contentRootPath)
    {
        if (!db.Database.IsSqlite())
        {
            return;
        }

        if (db.Users.Any(u => TestUserIds.Contains(u.UserId)) ||
            db.Shifts.Any(s => TestShiftIds.Contains(s.ShiftId)))
        {
            return;
        }

        var seedDirectory = Path.Combine(contentRootPath, "SeedData", "Sql", "QA");
        if (!Directory.Exists(seedDirectory))
        {
            seedDirectory = Path.Combine(AppContext.BaseDirectory, "SeedData", "Sql", "QA");
        }
        var seedFiles = new[]
        {
            Path.Combine(seedDirectory, "roles.sql"),
            Path.Combine(seedDirectory, "users.sql"),
            Path.Combine(seedDirectory, "shifts.sql")
        };

        foreach (var filePath in seedFiles)
        {
            ExecuteSqlFile(db, filePath);
        }
    }

    public static void SeedQaTestAccount(OrgDbContext db)
    {
        const string username = "qa_test";
        const string password = "123456";
        const string roleName = "Admin";

        if (!db.Account.Any(a => a.Username == username))
        {
            db.Account.Add(new Account
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = roleName,
                DisplayName = "QA Test",
                CreatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
        }
    }


    private static void ExecuteSqlFile(OrgDbContext db, string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Seed SQL file not found.", filePath);
        }

        var sql = File.ReadAllText(filePath);
        foreach (var statement in SplitStatements(sql))
        {
            db.Database.ExecuteSqlRaw(statement);
        }
    }

    private static IEnumerable<string> SplitStatements(string sql)
    {
        return sql
            .Split(';')
            .Select(statement => statement.Trim())
            .Where(statement => statement.Length > 0);
    }
}