using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;

using PropertyPortal.Repositories;
using PropertyPortal.Common;
using PropertyPortal.Services;

var builder = WebApplication.CreateBuilder(args);

// ================== Controllers & Filters ==================
builder.Services.AddControllers(opt =>
{
    opt.Filters.Add<ApiResponseFilter>();   // bọc mọi response (file gốc)
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ================== EF Core DbContext (file gốc) ==================
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// ================== JWT Authentication (THÊM MỚI) ==================
builder.Services
    .AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            ),
            ClockSkew = TimeSpan.Zero
        };
    });

// ================== Authorization theo Permission (THÊM MỚI) ==================
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();

// ================== DI Services (THÊM MỚI) ==================
builder.Services.AddScoped<IPermissionService, PermissionService>(); // Dapper/Npgsql
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();  // tạo JWT

// (Tuỳ chọn) CORS nếu FE React gọi khác domain
builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddControllers(opt => { opt.Filters.Add<ApiResponseFilter>(); });

var app = builder.Build();

// ================== Middlewares ==================
app.UseMiddleware<ExceptionMiddleware>();     // bọc lỗi (file gốc)

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();           // nếu dùng CORS
app.UseAuthentication(); // << BẮT BUỘC: trước Authorization
app.UseAuthorization();

app.MapControllers();

app.Run();
