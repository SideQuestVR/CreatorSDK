using NUnit.Framework;

namespace BS.SDKEditor.Tests
{
    /// <summary>Only the SideQuest CDN understands the resize parameter, so it must never go on anyone else's url.</summary>
    public class PortalImageTests
    {
        const string Cdn = "https://cdn.sidequestvr.com/file/567302/portalplaceholder_7.png";

        [Test]
        public void TheCdn_GetsTheSize() => Assert.AreEqual(Cdn + "?size=512", BSPortal.Sized(Cdn, 512));

        [Test]
        public void TheCdn_KeepsAQueryItAlreadyHas() => Assert.AreEqual(Cdn + "?v=2&size=512", BSPortal.Sized(Cdn + "?v=2", 512));

        [Test]
        public void ASizeAlreadyThere_IsLeftAlone() => Assert.AreEqual(Cdn + "?size=64", BSPortal.Sized(Cdn + "?size=64", 512));

        [TestCase("https://example.com/world.png")]
        [TestCase("https://cdn.sidequestvr.com.example.com/world.png")]
        [TestCase("not a url")]
        [TestCase("")]
        public void AnythingElse_IsLeftAlone(string url) => Assert.AreEqual(url, BSPortal.Sized(url, 512));

        // World images come back from /v2/worlds/by-url as CDN-relative paths.
        [Test]
        public void AWorldImagePath_BecomesACdnUrl() =>
            Assert.AreEqual("https://cdn.sidequestvr.com/file/4885846/card.png", Get.Cdn("file/4885846/card.png"));

        [Test]
        public void AWorldImagePath_WithALeadingSlash_BecomesACdnUrl() =>
            Assert.AreEqual("https://cdn.sidequestvr.com/file/4885846/card.png", Get.Cdn("/file/4885846/card.png"));

        [Test]
        public void AFullUrl_PassesThrough() => Assert.AreEqual("https://example.com/a.png", Get.Cdn("https://example.com/a.png"));

        // Unity can't decode WebP; the CDN converts to the type the file name asks for.
        [Test]
        public void AWebpWorldImage_IsAskedForAsPng() =>
            Assert.AreEqual("https://cdn.sidequestvr.com/file/1/card.png", Get.Cdn("file/1/card.webp"));

        [TestCase("https://cdn.sidequestvr.com/file/1/a.webp?size=512", "https://cdn.sidequestvr.com/file/1/a.png?size=512")]
        [TestCase("https://cdn.sidequestvr.com/file/1/a.png?size=512", "https://cdn.sidequestvr.com/file/1/a.png?size=512")]
        [TestCase("https://example.com/a.webp", "https://example.com/a.webp")]
        public void TextureDownloads_AskTheCdnForADecodableType(string url, string expected) =>
            Assert.AreEqual(expected, Get.Decodable(url));

        [TestCase("/assets/images/old.png")]
        [TestCase("")]
        [TestCase(null)]
        public void NoUsableImage_GivesNull(string path) => Assert.IsNull(Get.Cdn(path));
    }
}
