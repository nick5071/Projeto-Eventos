using Microsoft.EntityFrameworkCore;
using Projeto_eventos.Models;
using Microsoft.AspNetCore.Identity;
using MercadoPago.Config;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDbContext<Conexao>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
})
    .AddErrorDescriber<ClasseErros>()
    .AddEntityFrameworkStores<Conexao>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Home/Login";
    options.AccessDeniedPath = "/Home/AcessoNegado";
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSession();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

MercadoPagoConfig.AccessToken = "########";

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    string emailAdmin = "admin@evento.com";
    string senhaAdmin = "Admin123!";

    var adminUser = await userManager.FindByEmailAsync(emailAdmin);

    if (adminUser == null)
    {
        var novoAdmin = new IdentityUser
        {
            UserName = emailAdmin,
            Email = emailAdmin,
            EmailConfirmed = true
        };

        var resultado = await userManager.CreateAsync(novoAdmin, senhaAdmin);

        if (resultado.Succeeded)
            await userManager.AddToRoleAsync(novoAdmin, "Admin");
    }
}

app.Run();
