using Autofac;
using Autofac.Extensions.DependencyInjection;
using Demo_Autofac.Repositories;
using Demo_Autofac.Services;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

//ʹ��Autofac��ΪĬ��IOC����
//���ʹ��Setup.cs��Ϊ�������ã�����Ҫ��Setup.cs�д���  public void ConfigureContainer(ContainerBuilder builder){} ����
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory(builder =>
{
    //������������ �۲� MathService��UseMathService��BaseRepository �������Ĵ���
    builder.RegisterType<ResolveCountService>().As<IResolveCountService>().SingleInstance();

    //����ģʽ
    builder.RegisterType<MathService>().As<IMathService>().SingleInstance();

    //ÿ���������ᴴ���µ�ʵ��
    builder.RegisterType<MathService>().As<IMathService>().InstancePerDependency();

    //�����������������д���һ��
    builder.RegisterType<MathService>().As<IMathService>().InstancePerLifetimeScope();

    //���ݳ���������ɸѡ���ͽ�������ע��
    //IBaseRepository �� MathService�б�ע�� �۲����ʱ��Ҫ�ο�IMathService�Ľ���
    builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
           .PublicOnly()
           .Where(t => t.Name.EndsWith("Repository"))
           .Except<UnUseRepository>()
           .AsImplementedInterfaces()
           .InstancePerDependency();

    builder.RegisterType<UseMathService>().As<IUseMathService>().InstancePerLifetimeScope();


    //�������õ�ע�뷽ʽ

    //���Ͷ�̬ע�� 
    builder.RegisterGeneric(typeof(TemplateService<,>))
        .As(typeof(ITemplateService<,>));
}));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Demo_Autofac", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (builder.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Demo_Autofac v1"));
}

#region api

/// <summary>
/// ��������ע��ļ�����������
/// </summary>
app.MapGet("math/add", (double x1, double x2, [FromServices] IMathService mathService, [FromServices] IUseMathService useMathService) =>
{
useMathService.Add(x1, x2);

return Results.Ok(mathService.Add(x1, x2));
});

// ����ע�뷽ʽ
app.MapGet("test/others", ([FromServices] ITemplateService<int, string> treeBaseService) =>
{
    treeBaseService.GetNode(1);

    return Results.Ok();
}); 
#endregion

app.Run();
