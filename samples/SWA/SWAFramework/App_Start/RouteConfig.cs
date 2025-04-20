using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;

namespace SWAFramework;

public class RouteConfig
{
    public static void RegisterRoutes(RouteCollection routes)
    {
        routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
        routes.MapHandlerRoute<Handlers.SessionInfo>("framework", "framework");
        routes.MapHandlerRoute<Handlers.IsDebuggerAttached>("debug", "debug");
        routes.MapRoute(
            name: "Default",
            url: "{controller}/{action}/{id}",
            defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional },
            constraints: new { controller = "^(?!(framework)|(debug)).*" }
        );
    }
}

public static class RouteExtensions
{
    public static Route MapHandlerRoute<THandler>(this RouteCollection routes, string name, string path)
        where THandler : IRouteHandler, new()
    {
        var route = new Route(path, new RouteValueDictionary(), new RouteValueDictionary()
            {
                { "controller", "" }
            },
            null,
            new THandler());
        routes.Add(name, route);
        return route;
    }
}
