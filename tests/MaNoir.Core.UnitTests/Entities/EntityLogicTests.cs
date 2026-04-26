using MaNoir.Core.Contracts.Models.Entities;
using MaNoir.Core.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace MaNoir.Core.UnitTests.Entities;

[TestClass]
public sealed class EntityLogicTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void PrepareForSave_ShouldNormalizeIdentityAndClearProjectionSource()
    {
        Entity entity = new Entity()
        {
            Id = "ABC-123",
            EntityKind = " Core:Status ",
            Source = "projection/demo",
            Roles = null,
            Datas = null
        };

        EntityLogic.PrepareForSave(entity);

        Assert.AreEqual("abc-123", entity.Id);
        Assert.AreEqual("core:status", entity.EntityKind);
        Assert.IsNull(entity.Source);
        Assert.IsFalse(entity.IsReadOnly);
        Assert.IsNotNull(entity.Roles);
        Assert.IsNotNull(entity.Datas);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void NormalizeEntityKinds_ShouldDeduplicateAndDiscardEmptyValues()
    {
        List<string> normalizedKinds = EntityLogic.NormalizeEntityKinds([" Core:Status ", "core:status", null, "  ", "core:other"]);

        CollectionAssert.AreEqual(new List<string>() { "core:status", "core:other" }, normalizedKinds);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void IsReadOnly_ShouldReflectProjectionSource()
    {
        Entity nativeEntity = new Entity();
        Entity projectedEntity = new Entity() { Source = "projection/demo" };

        Assert.IsFalse(nativeEntity.IsReadOnly);
        Assert.IsTrue(projectedEntity.IsReadOnly);
    }
}