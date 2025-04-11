using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using AccreditationSystem.Pages.Services; // Make sure namespace matches your project

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Add session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register email service with secure credential management
builder.Services.Configure<EmailSettings>(options =>
{
    // Load base configuration from appsettings.json
    builder.Configuration.GetSection("EmailSettings").Bind(options);

    // Load credentials from environment variables or user secrets
    // This prioritizes secrets over appsettings values
    var smtpUsername = builder.Configuration["EmailSettings:SmtpUsername"];
    var smtpPassword = builder.Configuration["EmailSettings:SmtpPassword"];

    if (!string.IsNullOrEmpty(smtpUsername))
    {
        options.SmtpUsername = smtpUsername;
    }

    if (!string.IsNullOrEmpty(smtpPassword))
    {
        options.SmtpPassword = smtpPassword;
    }
});

builder.Services.AddTransient<IEmailService, EmailService>();

// Add logging service
builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    // In development, show detailed error pages
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Add session middleware - this must be before MapRazorPages
app.UseSession();

app.MapRazorPages();

app.Run();