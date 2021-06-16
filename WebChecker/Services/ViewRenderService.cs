using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AhDung.AspNet.Razor
{
    public interface IViewRenderService
    {
        Task<string> RenderToStringAsync(string viewName, object model);
    }

    public class ViewRenderService : IViewRenderService
    {
        readonly IRazorViewEngine _razorViewEngine;
        readonly ITempDataProvider _tempDataProvider;
        readonly ActionContext _actionContext;
        readonly HtmlHelperOptions _htmlHelperOptions = new();
        readonly EmptyModelMetadataProvider _emptyModelMetadataProvider = new();

        public ViewRenderService(
            IRazorViewEngine razorViewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider)
        {
            _razorViewEngine  = razorViewEngine;
            _tempDataProvider = tempDataProvider;
            var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
            _actionContext = new ActionContext(httpContext, new(), new());
        }

        public async Task<string> RenderToStringAsync(string viewPath, object model)
        {
            var viewResult = _razorViewEngine.GetView(null, viewPath, false);
            if (viewResult.View == null)
            {
                throw new ArgumentException($"{viewPath} does not match any available view");
            }

            await using var writer = new StringWriter();

            var viewDictionary = new ViewDataDictionary(_emptyModelMetadataProvider, new())
            {
                Model = model
            };

            var viewContext = new ViewContext(
                _actionContext,
                viewResult.View,
                viewDictionary,
                new TempDataDictionary(_actionContext.HttpContext, _tempDataProvider),
                writer,
                _htmlHelperOptions
            );

            await viewResult.View.RenderAsync(viewContext);
            return writer.ToString();
        }
    }

    public static class ViewRenderServiceExtensions
    {
        public static IServiceCollection TryAddViewRender(this IServiceCollection services)
        {
            services.TryAddScoped<IViewRenderService, ViewRenderService>();
            return services;
        }
    }
}