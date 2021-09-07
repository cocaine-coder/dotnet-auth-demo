using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Extras.DynamicProxy;
using Demo_Autofac.Aop;
using Demo_Autofac.Repositories;
using Demo_Autofac.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

//ʹ��Autofac��ΪĬ��IOC����
//���ʹ��Setup.cs��Ϊ�������ã�����Ҫ��Setup.cs�д���  public void ConfigureContainer(ContainerBuilder builder){} ����
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory(builder =>
{
    //������������ �۲� MathService��UseMathService��BaseRepository �������Ĵ���
    //����MathService��ע�� ���һ��ʵ�֣�����ʱ���Դ��ϵ���ע��
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

    //���Ͷ�̬ע�� ��ʵ��Aop
    builder.RegisterGeneric(typeof(TemplateService<,>))
        .As(typeof(ITemplateService<,>))
        .EnableInterfaceInterceptors();

    //ע�����ʵ�ֵ�aop
    builder.RegisterType(typeof(CustomAutofacAop));

    //ʹ��aopʵ��efcore�汾��unit of work
    //ע������������
    builder.RegisterType(typeof(UnitOfWorkInterceptor));

    //ע��uow
    builder.RegisterType<UnitOfWork>().As<IUnitOfWork>().InstancePerLifetimeScope();

    //ע��ʹ��uow��service
    builder.RegisterType<EFCoreAopStudentService>()
        .As<IEFCoreAopStudentService>()
        .InstancePerLifetimeScope()
        .EnableInterfaceInterceptors()
        .InterceptedBy(typeof(UnitOfWorkInterceptor));


    #region ע�����п������Ĺ�ϵ��������ʵ��������Ҫ�����

    var controllersTypesInAssembly = Assembly.GetExecutingAssembly().GetExportedTypes()
        .Where(type => typeof(ControllerBase).IsAssignableFrom(type)).ToArray();

    builder.RegisterTypes(controllersTypesInAssembly)
        .PropertiesAutowired();

    #endregion
}));

builder.Services.AddDbContext<AppDbContext>(builder =>
{
    builder.UseInMemoryDatabase("test");
});

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

/// <summary>
/// ����ע�뷽ʽ
/// </summary>
app.MapGet("test/others",async ([FromServices] ITemplateService<int, string> treeBaseService) =>
{
    treeBaseService.GetNode(1);

    return Results.Ok(await treeBaseService.GetKeyAsync(1));
});

#region Aop for ef uow 

app.MapGet("aop/students", async ([FromServices]IEFCoreAopStudentService studentService) =>
{
    return Results.Ok(await studentService.GetAsync());
});

app.MapGet("aop/students/{id}", async ([FromRoute] int id, [FromServices] IEFCoreAopStudentService studentService) =>
{
    return Results.Ok(await studentService.GetAsync(id));
});

app.MapPost("aop/students", async ([FromBody] Student student,[FromServices] IEFCoreAopStudentService studentService) =>
{
    await studentService.CreateAsync(student);
    return Results.Ok(await studentService.GetAsync());
});

app.MapPut("aop/students/{id}", async ([FromRoute] int id, [FromBody] string name, [FromServices] IEFCoreAopStudentService studentService) =>
{
    await studentService.UpdateAsync(id, name);
    return Results.Ok(await studentService.GetAsync());
});

app.MapDelete("aop/students/{id}", async ([FromRoute] int id,[FromServices] IEFCoreAopStudentService studentService) =>
{
    await studentService.DeleteAsync(id);
    return Results.Ok(await studentService.GetAsync());
});

#endregion

#endregion

app.Run();
