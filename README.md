

# . ASP.NET Core WebApi 使用 AspNetCoreRateLimit 限流控制

## 介绍

　AspNetCoreRateLimit是一个ASP.NET Core速率限制的解决方案，旨在控制客户端根据IP地址或客户端ID向Web API或MVC应用发出的请求的速率。AspNetCoreRateLimit包含一个IpRateLimitMiddleware和ClientRateLimitMiddleware，每个中间件可以根据不同的场景配置限制允许IP或客户端，自定义这些限制策略，也可以将限制策略应用在每​​个API URL或具体的HTTP Method上。


> 源码地址：https://github.com/stefanprodan/AspNetCoreRateLimit
> 实例地址：https://github.com/xteamwx/IpRateLimitingExample

## 快速入门

### 第一步：下载AspNetCoreRateLimit

使用Nuget下载最新的`AspNetCoreRateLimit`库

```c#
PM> Install-Package AspNetCoreRateLimit 
```

### 第二步：添加AspNetCoreRateLimit服务

在`Startup.cs`文件中添加AspNetCoreRateLimit服务
```c#
public void ConfigureServices(IServiceCollection services) 
{   
     //需要存储速率限制计算器和ip规则
     services.AddMemoryCache();

     //从appsettings.json中加载常规配置
     services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));

     //从appsettings.json中加载Ip规则   IP可以不配置，后期继续成本太高
     services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"));

     //注入计数器和规则存储
     services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
     services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();

     services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
     //配置（解析器、计数器密钥生成器）
     services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();


     services.AddControllers();
     services.AddSwaggerGen(c =>
     {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "IpRateLimitingExample", Version = "v1" });
     });
}
```

修改Configure方法

```c#
  public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "IpRateLimitingExample v1"));
            }

            app.UseHttpsRedirection();

            //限流配置，两种模式二选一
            //模式一 ：启用客户端IP限流
            app.UseIpRateLimiting();
            //模式二 ：启用客户端ID限流
            //app.UseClientRateLimiting();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
```

### 第三步：配置appsettings.json 

 
```c#
  {
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "*",

  "IpRateLimiting": {
    //false，则全局将应用限制，并且仅应用具有作为端点的规则*。例如，如果您设置每秒5次调用的限制，则对任何端点的任何HTTP调用都将计入该限制
    //true， 则限制将应用于每个端点，如{HTTP_Verb}{PATH}。例如，如果您为*:/api/values客户端设置每秒5个呼叫的限制，
    "EnableEndpointRateLimiting": false,
    //false，拒绝的API调用不会添加到调用次数计数器上;如 客户端每秒发出3个请求并且您设置了每秒一个调用的限制，则每分钟或每天计数器等其他限制将仅记录第一个调用，即成功的API调用。如果您希望被拒绝的API调用计入其他时间的显示（分钟，小时等），则必须设置StackBlockedRequests为true。
    "StackBlockedRequests": false,
    //Kestrel 服务器背后是一个反向代理，如果你的代理服务器使用不同的页眉然后提取客户端IP X-Real-IP使用此选项来设置
    "RealIpHeader": "X-Real-IP",
    //取白名单的客户端ID。如果此标头中存在客户端ID并且与ClientWhitelist中指定的值匹配，则不应用速率限制。
    "ClientIdHeader": "X-ClientId",
    //限制状态码
    "HttpStatusCode": 429,
    ////IP白名单:支持Ip v4和v6 
    //"IpWhitelist": [ "127.0.0.1", "::1/10", "192.168.0.0/24" ],
    ////端点白名单:支持各种请求；
    //"EndpointWhitelist": [ "get:/api/license", "*:/api/status" ],
    ////客户端白名单
    //"ClientWhitelist": [ "dev-id-1", "dev-id-2" ],
    //通用规则
    "GeneralRules": [
      {
        //端点路径
        "Endpoint": "*",
        //时间段，格式：{数字}{单位}；可使用单位：s, m, h, d
        "Period": "1s",
        //限制
        "Limit": 1
      },
      //15分钟只能调用100次
      {
        "Endpoint": "*",
        "Period": "15m",
        "Limit": 100
      },
      //12H只能调用1000
      {
        "Endpoint": "*",
        "Period": "12h",
        "Limit": 1000
      },
      //7天只能调用10000次
      {
        "Endpoint": "*",
        "Period": "7d",
        "Limit": 10000
      }
    ]
  },
  "IpRateLimitPolicies": {
    //ip规则
    "IpRules": [
      {
        //IP
        "Ip": "127.0.0.1",
        //规则内容
        "Rules": [
          //1s请求10次
          {
            "Endpoint": "*",
            "Period": "1s",
            "Limit": 10
          },
          //15分钟请求200次
          {
            "Endpoint": "*",
            "Period": "15m",
            "Limit": 200
          }
        ]
      },
      {
        //ip支持设置多个
        "Ip": "192.168.3.22/25",
        "Rules": [
          //1秒请求5次
          {
            "Endpoint": "*",
            "Period": "1s",
            "Limit": 5
          },
          //15分钟请求150次
          {
            "Endpoint": "*",
            "Period": "15m",
            "Limit": 150
          },
          //12小时请求500次
          {
            "Endpoint": "*",
            "Period": "12h",
            "Limit": 500
          }
        ]
      }
    ]
  },
  "ClientRateLimitPolicies": {
    "ClientRules": [
      {
        "ClientId": "client-id-1",
        "Rules": [
          {
            "Endpoint": "*",
            "Period": "1s",
            "Limit": 10
          },
          {
            "Endpoint": "*",
            "Period": "15m",
            "Limit": 200
          }
        ]
      },
      {
        "ClientId": "client-id-2",
        "Rules": [
          {
            "Endpoint": "*",
            "Period": "1s",
            "Limit": 5
          },
          {
            "Endpoint": "*",
            "Period": "15m",
            "Limit": 150
          },
          {
            "Endpoint": "*",
            "Period": "12h",
            "Limit": 500
          }
        ]
      }
    ]
  }
}

```


### 第四步：启动项目

启动项目  访问https://localhost:44389/swagger/index.html ，点我们连续多次点击执行时就可看到如下所示的限制的结果

![](http://www.helink-iot.com/Blogimages/AspNetCoreRateLimit.png)




至此，在 ASP.NET Core中集成AspNetCoreRateLimit就完成了。

## 总结

本篇讲解了如何在Web API项目中使用`AspNetCoreRateLimit`进行限流控制，更多验证方法请参考官方文档。

本篇源代码 https://github.com/xteamwx/IpRateLimitingExample

> 作者：SAMURAI
> 出处： 原创
> 版权：本作品采用「署名-非商业性使用-相同方式共享 4.0 国际」许可协议进行许可。
