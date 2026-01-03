//分Git库
using AspectCore.Extensions.DependencyInjection;
using Byte.Core.Api;
using Byte.Core.Api.Common;
using Byte.Core.Api.Quartz;
using Byte.Core.Common.Extensions;

var builder = BuildApplication.Build();
builder.Services.BuildAspectCoreWithAutofacServiceProvider();//builder 注入结束
builder.Host.UseServiceProviderFactory(new DynamicProxyServiceProviderFactory());//动态代理
var app = builder.Build();
BuildApplication.App(app, builder);
#region 初始数据库
await app.UseDataSeederMiddlewareAsync("Byte.Core.Entity");
#endregion
#region  任务调度
await app.QuartzJobInit();
#endregion
app.Run();