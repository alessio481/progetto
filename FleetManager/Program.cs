using FleetManager;
using FleetManager.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Usiamo MVC classico: controller C# + view Razor.
builder.Services.AddControllersWithViews();

// Il progetto usa SQL Server / LocalDB.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});

// Login semplice con cookie di autenticazione.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
    });

var app = builder.Build();

// All'avvio prepariamo il database demo.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    await DemoDataSeeder.PreparaDatabaseDemoAsync(db);

    // Se il database arriva da una versione vecchia, puliamo alcune colonne che non usiamo piu.
    await RimuoviColonnaSeEsisteAsync(db, "Veicoli", "Colore");
    await RimuoviColonnaSeEsisteAsync(db, "Veicoli", "Cilindrata");
    await RimuoviColonnaSeEsisteAsync(db, "Veicoli", "UtenteManutentoreID");
    await RimuoviColonnaSeEsisteAsync(db, "Veicoli", "PostoAuto");
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

static async Task RimuoviColonnaSeEsisteAsync(ApplicationDbContext db, string nomeTabella, string nomeColonna)
{
    // Prima togliamo eventuali vincoli o indici collegati, poi eliminiamo la colonna.
    var sql = $"""
        IF COL_LENGTH('{nomeTabella}', '{nomeColonna}') IS NOT NULL
        BEGIN
            DECLARE @sql nvarchar(max) = N'';

            SELECT @sql += N'ALTER TABLE [{nomeTabella}] DROP CONSTRAINT [' + dc.name + '];'
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
            WHERE dc.parent_object_id = OBJECT_ID('{nomeTabella}')
              AND c.name = '{nomeColonna}';

            SELECT @sql += N'ALTER TABLE [{nomeTabella}] DROP CONSTRAINT [' + fk.name + '];'
            FROM sys.foreign_keys fk
            INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
            INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
            WHERE fk.parent_object_id = OBJECT_ID('{nomeTabella}')
              AND c.name = '{nomeColonna}';

            SELECT @sql += N'DROP INDEX [' + i.name + '] ON [{nomeTabella}];'
            FROM sys.indexes i
            INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE i.object_id = OBJECT_ID('{nomeTabella}')
              AND i.is_primary_key = 0
              AND i.is_unique_constraint = 0
              AND c.name = '{nomeColonna}';

            IF LEN(@sql) > 0
                EXEC sp_executesql @sql;

            EXEC ('ALTER TABLE [{nomeTabella}] DROP COLUMN [{nomeColonna}]');
        END
        """;

    await db.Database.ExecuteSqlRawAsync(sql);
}
