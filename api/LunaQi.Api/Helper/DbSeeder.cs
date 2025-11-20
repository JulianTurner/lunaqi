using LunaQi.Api.Data;
using LunaQi.Api.Models;

namespace LunaQi.Api.Helper;

public static class DbSeeder
{
    public static void Seed(LunaQiDbContext db)
    {
        if (!db.Users.Any())
        {
            var user = new User
            {
                Username = "julian",
                Id = Guid.Parse("8dbc59d9-6899-4e8d-9bfb-6bb23a0207dd"),
                PasswordHash = "952EF091A3994EFFFB338A6A04D7F965:B50BB96C0DFD9C3D88F988C7A2B4C8BF00CA13F9F99C400392E5B5EB44094818",
                UserPhases = new List<UserPhase>(),
                Region = "europe",
            };
            
            db.Users.Add(user);
            db.SaveChanges();
        }

        if (!db.PhaseDefinitions.Any())
        {
            var winter = new PhaseDefinition
            {
                Id = Guid.NewGuid(),
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(3),
                Name = "Winter",
            };
            
            var spring = new PhaseDefinition
            {
                Id = Guid.NewGuid(),
                StartDate = DateTime.Now.AddMonths(3),
                EndDate = DateTime.Now.AddMonths(6),
                Name = "Spring",
            };
            
            db.PhaseDefinitions.AddRange(winter, spring);
            db.SaveChanges();
            
        }

        if (!db.UserPhases.Any())
        {
            var adminUser = db.Users.First(u => u.Username == "julian");
            var winterPhase = db.PhaseDefinitions.First(p => p.Name == "Winter");
            var springPhase = db.PhaseDefinitions.First(p => p.Name == "Spring");
            
            var adminWinterPhase = new UserPhase
            {
                UserId = adminUser.Id,
                PhaseDefinitionId = winterPhase.Id,
                IsEnabled = true,
            };
            
            var adminSpringPhase = new UserPhase
            {
                UserId = adminUser.Id,
                PhaseDefinitionId = springPhase.Id,
                IsEnabled = false,
            };
            
            db.UserPhases.AddRange(adminWinterPhase, adminSpringPhase);
            db.SaveChanges();
        }

        
        

    }
}