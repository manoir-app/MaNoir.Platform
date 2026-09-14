using MaNoir.Core.AdminUi.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MaNoir.Core.UnitTests.AdminUi;

[TestClass]
public sealed class AdminUiHostingRewriteTests
{
    [TestMethod]
    public void ResolveRouterBasePath_ShouldUsePublicBasePathForRootSpa()
    {
        string routerBasePath = AdminUiHostingRewrite.ResolveRouterBasePath("/", "/home-automation");

        // No trailing slash: React Router basename must equal window.location.pathname exactly.
        Assert.AreEqual("/home-automation", routerBasePath);
    }

    [TestMethod]
    public void ResolveRouterBasePath_ShouldCombinePublicBasePathWithExplicitSpaBasePath()
    {
        string routerBasePath = AdminUiHostingRewrite.ResolveRouterBasePath("/front", "/platform");

        Assert.AreEqual("/platform/front", routerBasePath);
    }

    [TestMethod]
    public void ResolveRouterBasePath_ShouldKeepExplicitSpaBasePathWhenNoPublicBasePathExists()
    {
        string routerBasePath = AdminUiHostingRewrite.ResolveRouterBasePath("/front", publicBasePath: null);

        Assert.AreEqual("/front", routerBasePath);
    }

    [TestMethod]
    public void ResolveAssetPrefix_ShouldCombinePublicBasePathAndSpaFolder()
    {
        string assetPrefix = AdminUiHostingRewrite.ResolveAssetPrefix("front", "/platform");

        Assert.AreEqual("/platform/front/", assetPrefix);
    }

    [TestMethod]
    public void ResolveAssetPrefix_ShouldUseRootWhenNoPublicBasePathAndNoSpaFolderExist()
    {
        string assetPrefix = AdminUiHostingRewrite.ResolveAssetPrefix(spaFolder: null, publicBasePath: "/");

        Assert.AreEqual("/", assetPrefix);
    }

    [TestMethod]
    public void ResolveAssetPrefix_ShouldUseSpaFolderAloneWhenNoPublicBasePathExists()
    {
        string assetPrefix = AdminUiHostingRewrite.ResolveAssetPrefix("bootstrap", publicBasePath: null);

        Assert.AreEqual("/bootstrap/", assetPrefix);
    }
}