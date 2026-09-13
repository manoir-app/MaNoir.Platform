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

        Assert.AreEqual("/home-automation/", routerBasePath);
    }

    [TestMethod]
    public void ResolveRouterBasePath_ShouldPreserveExplicitSpaBasePath()
    {
        string routerBasePath = AdminUiHostingRewrite.ResolveRouterBasePath("/front", "/platform");

        Assert.AreEqual("/front/", routerBasePath);
    }

    [TestMethod]
    public void RewriteRootSpaAssetReferences_ShouldRewriteRelativeAndAbsoluteReferences()
    {
        string indexHtml = "<script src=\"./assets/app.js\"></script><link href='/assets/app.css'>";

        string rewrittenHtml = AdminUiHostingRewrite.RewriteRootSpaAssetReferences(indexHtml, "/home-automation/");

        StringAssert.Contains(rewrittenHtml, "src=\"/home-automation/assets/app.js");
        StringAssert.Contains(rewrittenHtml, "href='/home-automation/assets/app.css");
    }

    [TestMethod]
    public void RewriteRootSpaAssetReferences_ShouldKeepRootReferencesWhenNoPublicBasePathExists()
    {
        string indexHtml = "<script src=\"./assets/app.js\"></script>";

        string rewrittenHtml = AdminUiHostingRewrite.RewriteRootSpaAssetReferences(indexHtml, "/");

        StringAssert.Contains(rewrittenHtml, "src=\"/assets/app.js");
    }
}