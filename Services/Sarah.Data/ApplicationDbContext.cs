using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using Sarah.API.Interfaces.Service;
using Sarah.Data.Models;

namespace Sarah.Data
{
    public class ApplicationDbContext : IdentityDbContext<User, Role, string>, IDBService
    {
        public static string DatabaseFileName => "./InteLuk.db";

        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        protected ApplicationDbContext()
        {
            
        }

        public static IDBService CreateDefault()
        {
            DbContextOptionsBuilder builder = new DbContextOptionsBuilder();
            builder.UseSqlite("Filename=" + DatabaseFileName);
            return new ApplicationDbContext(builder.Options);
        }



        public DbSet<DeviceInfo> Devices => this.Set<DeviceInfo>();
        public DbSet<Room> Rooms => this.Set<Room>();
        public DbSet<DeviceTraceEntry> Traces => this.Set<DeviceTraceEntry>();
        public DbSet<PersonInfo> Persons => this.Set<PersonInfo>();
        public DbSet<TemperatureSchedule> TemperatureSchedules => this.Set<TemperatureSchedule>();
        public DbSet<DeseaseKpi> DeseaseStats => this.Set<DeseaseKpi>();
        public DbSet<RuleInfo> RuleInfo => this.Set<RuleInfo>();
        public DbSet<AlarmSchedule> AlarmSchedule => this.Set<AlarmSchedule>();




        // public static async Task<bool> CreateAdminOnFirstLaunch(UserManager<User> userManager, RoleManager<Role> roleManager)
        // {
        //     bool isCreated = false;
        //     if (userManager != null && roleManager != null)
        //     {
        //         User cf = await userManager.FindByNameAsync("cf");
        //         if (cf == null)
        //         {
        //             cf = new User()
        //             {
        //                 UserName = "cf",
        //                 Email = "c.fonden@die-rooter.de",
        //                 NormalizedEmail = "c.fonden@die-rooter.de",
        //                 EmailConfirmed = true,
        //                 SecurityStamp = Guid.NewGuid().ToString()
        //             };

        //             string hash = userManager.PasswordHasher.HashPassword(cf, "INITIAL");
        //             cf.PasswordHash = hash;
        //             IdentityResult res = await userManager.CreateAsync(cf);
        //             if (res.Succeeded)
        //             {
        //                 isCreated = true;
        //             }
        //         }

        //         if(! await roleManager.RoleExistsAsync(Role.AdminRoleName))
        //         {
        //             Role admins = new Role() { Name = Role.AdminRoleName, NormalizedName = Role.AdminRoleName.ToUpper() };
        //             IdentityResult res = await roleManager.CreateAsync(admins);
        //         }

        //         if (!await roleManager.RoleExistsAsync(Role.UsersRoleName))
        //         {
        //             Role admins = new Role() { Name = Role.UsersRoleName, NormalizedName = Role.UsersRoleName.ToUpper() };
        //             IdentityResult res = await roleManager.CreateAsync(admins);
        //         }

        //         if (!await userManager.IsInRoleAsync(cf, Role.AdminRoleName))
        //         {
        //             IdentityResult res = await userManager.AddToRoleAsync(cf, Role.AdminRoleName);
        //             if (!res.Succeeded)
        //             {
        //                 isCreated = false;
        //             }
        //         }
        //     }
        //     return isCreated;
        // }


    }

}
