using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DoomRu;
using DoomRu.Data;
using DoomRu.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// ---------- БД PostgreSQL ----------
builder.Services.AddDbContext<DoomRuContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DoomRuContext")));

// ---------- Identity (авторизация по номеру телефона) ----------
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.SignIn.RequireConfirmedAccount = false;

    // не требуем уникального email
    options.User.RequireUniqueEmail = false;
})
.AddEntityFrameworkStores<DoomRuContext>()
.AddDefaultTokenProviders();

// ---------- Razor Pages ----------
builder.Services.AddRazorPages(options =>
{
    // Публичные страницы
    options.Conventions.AllowAnonymousToPage("/Index");
    options.Conventions.AllowAnonymousToPage("/SpeedTest");
    options.Conventions.AllowAnonymousToPage("/Privacy");
    options.Conventions.AllowAnonymousToPage("/Auth/Login");
    options.Conventions.AllowAnonymousToPage("/Auth/Register");
});

// ---------- Контроллеры (для API чата и спидтеста) ----------
builder.Services.AddControllers();

// ---------- Сервис чата с ИИ ----------
builder.Services.AddHttpClient();
builder.Services.AddScoped<AiChatService>();

var app = builder.Build();

// ---------- Middleware ----------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

// ---------- Автосоздание БД при первом запуске ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DoomRuContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();