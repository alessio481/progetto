using FleetManager;
using FleetManager.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
    });

var app = builder.Build();

// All'avvio creiamo il DB se manca e carichiamo i dati demo solo se serve.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DemoDataSeeder.EnsureReadyAsync(db);
    // Se il DB arriva da versioni vecchie, togliamo in modo sicuro le colonne legacy.
    await DropColumnIfExistsAsync(db, "Veicoli", "Colore");
    await DropColumnIfExistsAsync(db, "Veicoli", "Cilindrata");
    await DropColumnIfExistsAsync(db, "Veicoli", "UtenteManutentoreID");
    await DropColumnIfExistsAsync(db, "Veicoli", "PostoAuto");
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static async Task DropColumnIfExistsAsync(ApplicationDbContext db, string tableName, string columnName)
{
    var sql = $"""
        IF COL_LENGTH('{tableName}', '{columnName}') IS NOT NULL
        BEGIN
            DECLARE @sql nvarchar(max) = N'';

            SELECT @sql += N'ALTER TABLE [{tableName}] DROP CONSTRAINT [' + dc.name + '];'
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
            WHERE dc.parent_object_id = OBJECT_ID('{tableName}')
              AND c.name = '{columnName}';

            SELECT @sql += N'ALTER TABLE [{tableName}] DROP CONSTRAINT [' + fk.name + '];'
            FROM sys.foreign_keys fk
            INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
            INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
            WHERE fk.parent_object_id = OBJECT_ID('{tableName}')
              AND c.name = '{columnName}';

            SELECT @sql += N'DROP INDEX [' + i.name + '] ON [{tableName}];'
            FROM sys.indexes i
            INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE i.object_id = OBJECT_ID('{tableName}')
              AND i.is_primary_key = 0
              AND i.is_unique_constraint = 0
              AND c.name = '{columnName}';

            IF LEN(@sql) > 0
                EXEC sp_executesql @sql;

            EXEC ('ALTER TABLE [{tableName}] DROP COLUMN [{columnName}]');
        END
        """;

    await db.Database.ExecuteSqlRawAsync(sql);
}
