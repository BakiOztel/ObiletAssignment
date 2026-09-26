using System.Threading.RateLimiting;
using BusJourney.Application;
using BusJourney.Application.Abstractions;
using BusJourney.Infrastructure;
using BusJourney.Web.Filters;
using BusJourney.Web.Session;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<ProviderExceptionFilter>();

    // Values that cannot be parsed (e.g. a hand-edited URL with date=2026-09-39) get a Turkish message,
    // not the framework's English default.
    var messages = options.ModelBindingMessageProvider;
    messages.SetAttemptedValueIsInvalidAccessor((_, field) =>
        field == "date" ? "Geçerli bir tarih girin." : "Arama bilgileri geçersiz.");
    messages.SetValueMustBeANumberAccessor(_ => "Arama bilgileri geçersiz.");
    messages.SetValueIsInvalidAccessor(_ => "Arama bilgileri geçersiz.");
});

// Each visitor's provider session is kept in ASP.NET Core Session (cookie + IDistributedCache).
// ponytail: in-memory cache works for a single instance; use AddStackExchangeRedisCache when scaling out.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".BusJourney.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(20);
});
// Every request without a session cookie opens a new provider session; limit per client IP so the
// provider quota and the session cache cannot be exhausted by a script.
// ponytail: RemoteIpAddress is the proxy's address behind a reverse proxy; add UseForwardedHeaders there.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 100, Window = TimeSpan.FromMinutes(1) }));
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IProviderSessionStore, HttpSessionProviderSessionStore>();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.Use((context, next) =>
{
    var headers = context.Response.Headers;
    headers.XContentTypeOptions = "nosniff";
    headers.XFrameOptions = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    return next(context);
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRequestLocalization("tr-TR");
app.UseRouting();
app.UseRateLimiter();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
