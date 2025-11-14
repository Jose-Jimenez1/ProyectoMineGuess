var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


var app = builder.Build();


// Configure API usage (no visual changes)
var useApi = builder.Configuration.GetSection("Api").GetValue<bool>("UseApi");
var apiBase = builder.Configuration.GetSection("Api").GetValue<string>("ApiBaseUrl");
MVP_ProyectoFinal.Models.RepositorioBloques.ConfigurarApi(useApi, apiBase);
MVP_ProyectoFinal.Models.RepositorioEntidades.ConfigurarApi(useApi, apiBase);


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();