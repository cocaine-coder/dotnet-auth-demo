using Demo_Jwt;
using Demo_Jwt.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddJwtAuthentication(builder.Configuration)
    .AddCustomAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IJwtService, JwtService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Demo", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Description = "���¿�����������ͷ����Ҫ���Jwt��ȨToken��Bearer Token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme{
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            System.Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (builder.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Demo v1"));
}

app.UseAuthentication();
app.UseAuthorization();

#region api

/// <summary>
/// ģ��䷢token
/// �����У��ܾ��ṩrole��isForever�ؼ���
/// SecurityJwtConfig.Foreverָ����token������Ч<see cref="AuthExtension"/>
/// ȷ����ݺ�䷢token,token ��Я���û�id���û�������Ϣ(�ɹ�����)
/// </summary>
app.MapGet("token", ([FromQuery] Roles role, [FromQuery] bool isForever, IJwtService jwtService) =>
{
    var claims = new List<Claim>()
    {
        new Claim(JwtRegisteredClaimNames.Sub, "foo"),
        new Claim(ClaimTypes.Role, role.ToString())
    };

    if (isForever)
        claims.Add(new Claim(SecurityJwtConfig.Forever, ""));

    return Results.Ok(jwtService.GenerateToken(claims));
});


#region ������֤��ʽ

/// <summary>
/// token������url����Ϊ�������ݣ�����һЩ���������Դʹ��
/// ���ｫaccess_token��ʾ�ط�����action���������У�����swagger����
/// </summary>
app.MapGet("token_in_url", [Authorize]([FromQuery] string access_token, IJwtService jwtService) =>
{
    return Results.Ok(jwtService.ResolveToken(access_token));
});

/// <summary>
/// token������request��header��
/// </summary>
app.MapGet("token_in_header", [Authorize](IHttpContextAccessor accessor, IJwtService jwtService) =>
{
    StringValues token = new();
    var ret = accessor.HttpContext?.Request.Headers.TryGetValue("Authorization", out token);
    if (ret == null || !ret.Value)
        return Results.BadRequest("request header can not find Authorization option!");

    return Results.Ok(jwtService.ResolveToken(token.First().Split(' ').Last()));
});

#endregion

#region ������Ȩ

app.MapGet("authorize/root", [Authorize(policy: nameof(Roles.ROOT))](string access_token) =>
{
    return Results.Ok(Roles.ROOT);
});

app.MapGet("authorize/admin", [Authorize(policy: nameof(Roles.ADMIN))](string access_token) =>
{
    return Results.Ok(Roles.ADMIN);
});

app.MapGet("authorize/normal", [Authorize(policy: nameof(Roles.NORMAL))](string access_token) =>
{
    return Results.Ok(Roles.NORMAL);
});

#endregion 

#endregion

app.Run();
