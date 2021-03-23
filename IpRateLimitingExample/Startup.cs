using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IpRateLimitingExample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            
            //��Ҫ�洢�������Ƽ�������ip����
            services.AddMemoryCache();

            //��appsettings.json�м��س�������
            services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));

            //��appsettings.json�м���Ip����   IP���Բ����ã����ڼ����ɱ�̫��
            services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"));

            //ע��������͹���洢
            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            //���ã�����������������Կ��������
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();


            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "IpRateLimitingExample", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "IpRateLimitingExample v1"));
            }

            app.UseHttpsRedirection();

            //�������ã�����ģʽ��ѡһ
            //ģʽһ �����ÿͻ���IP����
            app.UseIpRateLimiting();
            //ģʽ�� �����ÿͻ���ID����
            //app.UseClientRateLimiting();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
