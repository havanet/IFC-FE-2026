using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using IFC.Facturacion.Infrastructure.Dian.Xml.DianExtensions;
using IFC.Facturacion.Infrastructure.Dian.Xml.Invoice;

namespace IFC.Facturacion.Tests.Dian;

public sealed class InvoiceXmlTests
{
    private const string InvoiceNamespace = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
    private const string BasicNamespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private const string DianNamespace = "dian:gov:co:facturaelectronica:Structures-2-1";

    [Fact]
    public void OfficialSources_MatchRecordedHashes()
    {
        var schemas = Path.Combine(AppContext.BaseDirectory, "Schemas", "V1_9");
        using var provenance = JsonDocument.Parse(File.ReadAllText(Path.Combine(schemas, "provenance.json")));
        var hashes = provenance.RootElement.GetProperty("xsdSha256");
        Assert.Equal(20, hashes.EnumerateObject().Count());
        Assert.Equal(20, Directory.GetFiles(schemas, "*.xsd", SearchOption.AllDirectories).Length);

        foreach (var entry in hashes.EnumerateObject())
        {
            var path = Path.Combine(schemas, entry.Name.Replace('/', Path.DirectorySeparatorChar));
            Assert.Equal(entry.Value.GetString(), Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))));
        }

        Assert.Equal(provenance.RootElement.GetProperty("fixtureSha256").GetString(),
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(FixturePath))));
    }

    [Fact]
    public void OfficialSchemas_CompileWithLocalDependencies()
    {
        var schemas = LoadSchemas();

        Assert.True(schemas.IsCompiled);
        Assert.True(schemas.Contains(InvoiceNamespace));
        Assert.True(schemas.Contains(DianNamespace));
    }

    [Fact]
    public void OfficialInvoice_ConformsToUblSchema()
    {
        AssertSchemaValid(XDocument.Load(FixturePath), includeDianExtensions: false);
    }

    [Fact]
    public void OfficialInvoice_ReportsKnownDianSchemaDiscrepancy()
    {
        var errors = Validate(XDocument.Load(FixturePath));

        Assert.Equal(2, errors.Count);
        Assert.All(errors, error =>
        {
            Assert.Contains("schemeID", error);
            Assert.Contains("Enumeration", error, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void OfficialInvoice_DeserializesWithDecimalAmountsAndDianExtension()
    {
        var invoice = ReadInvoice();
        var original = XDocument.Load(FixturePath);
        XNamespace basic = BasicNamespace;
        XNamespace aggregate = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";

        Assert.Equal(original.Root!.Element(basic + "ID")!.Value, invoice.Id.Value);
        Assert.Equal(2, invoice.InvoiceLine.Count);
        Assert.Equal(
            decimal.Parse(original.Root.Element(aggregate + "LegalMonetaryTotal")!
                .Element(basic + "PayableAmount")!.Value, CultureInfo.InvariantCulture),
            invoice.LegalMonetaryTotal.PayableAmount.Value);
        Assert.Equal(2, invoice.UblExtensions.Count);

        var element = invoice.UblExtensions[0].ExtensionContent.Any;
        Assert.Equal(DianNamespace, element.NamespaceURI);
        using var reader = new XmlNodeReader(element);
        var extension = Assert.IsType<DianExtensionsType>(
            new XmlSerializer(typeof(DianExtensionsType)).Deserialize(reader));
        Assert.Equal(18760000001m, extension.InvoiceControl.InvoiceAuthorization.Value);
        Assert.NotNull(extension.SoftwareProvider.SoftwareId);
        Assert.Equal("4", extension.SoftwareProvider.ProviderId.SchemeId);
        Assert.Equal("31", extension.SoftwareProvider.ProviderId.SchemeName);

        using var serialized = new MemoryStream();
        new XmlSerializer(typeof(DianExtensionsType)).Serialize(serialized, extension);
        serialized.Position = 0;
        var extensionXml = XDocument.Load(serialized);
        var provider = extensionXml.Root!.Element(XName.Get("SoftwareProvider", DianNamespace))!
            .Element(XName.Get("ProviderID", DianNamespace))!;
        Assert.Equal("4", provider.Attribute("schemeID")!.Value);
        Assert.Equal("31", provider.Attribute("schemeName")!.Value);
    }

    [Fact]
    public void OfficialInvoice_RoundTripPreservesDatesOffsetsDecimalsAndExtensions()
    {
        var invoice = ReadInvoice();
        var roundTrip = RoundTrip(invoice);
        var original = XDocument.Load(FixturePath);
        XNamespace basic = BasicNamespace;

        Assert.Equal(original.Root!.Element(basic + "IssueDate")!.Value,
            roundTrip.Root!.Element(basic + "IssueDate")!.Value);
        Assert.Equal(original.Root.Element(basic + "IssueTime")!.Value,
            roundTrip.Root.Element(basic + "IssueTime")!.Value);
        Assert.Equal(original.Descendants(basic + "PayableAmount").Single().Value,
            roundTrip.Descendants(basic + "PayableAmount").Single().Value);
        Assert.Single(roundTrip.Descendants(XName.Get("DianExtensions", DianNamespace)));
        Assert.Single(roundTrip.Descendants(XName.Get("Signature", "http://www.w3.org/2000/09/xmldsig#")));
        AssertSchemaValid(roundTrip, includeDianExtensions: false);
    }

    [Fact]
    public void InvoiceWithoutId_FailsSchemaValidation()
    {
        var invoice = XDocument.Load(FixturePath);
        invoice.Root!.Element(XName.Get("ID", BasicNamespace))!.Remove();

        var errors = Validate(invoice, includeDianExtensions: false);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void InvoiceWithInvalidAmount_FailsUblSchemaValidation()
    {
        var invoice = XDocument.Load(FixturePath);
        invoice.Descendants(XName.Get("PayableAmount", BasicNamespace)).Single().Value = "not-a-number";

        Assert.NotEmpty(Validate(invoice, includeDianExtensions: false));
    }

    [Fact]
    public void DecimalAmount_RoundTripPreservesPrecision()
    {
        const decimal amount = 123456789012345.123456789m;
        var invoice = ReadInvoice();
        invoice.LegalMonetaryTotal.PayableAmount.Value = amount;

        var roundTrip = RoundTrip(invoice);
        var value = roundTrip.Descendants(XName.Get("PayableAmount", BasicNamespace)).Single().Value;

        Assert.Equal(amount.ToString(CultureInfo.InvariantCulture), value);
        AssertSchemaValid(roundTrip, includeDianExtensions: false);
    }

    private static string FixturePath =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Dian", "Generica.xml");

    private static InvoiceType ReadInvoice()
    {
        using var reader = XmlReader.Create(FixturePath,
            new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        return Assert.IsType<InvoiceType>(new XmlSerializer(typeof(InvoiceType)).Deserialize(reader));
    }

    private static XDocument RoundTrip(InvoiceType invoice)
    {
        using var output = new MemoryStream();
        using (var writer = XmlWriter.Create(output,
                   new XmlWriterSettings { Indent = true, CloseOutput = false }))
        {
            new XmlSerializer(typeof(InvoiceType)).Serialize(writer, invoice);
        }
        output.Position = 0;
        return XDocument.Load(output);
    }

    private static XmlSchemaSet LoadSchemas(bool includeDianExtensions = true)
    {
        var root = Path.Combine(AppContext.BaseDirectory, "Schemas", "V1_9", "maindoc");
        var schemas = new XmlSchemaSet { XmlResolver = new XmlUrlResolver() };
        schemas.ValidationEventHandler += (_, args) => throw args.Exception;
        // The official XMLDSig schema has an internal DTD; do not resolve external entities.
        using (var signature = XmlReader.Create(
                   Path.Combine(root, "..", "common", "UBL-xmldsig-core-schema-2.1.xsd"),
                   new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null }))
        {
            schemas.Add(null, signature);
        }

        schemas.Add(InvoiceNamespace, Path.Combine(root, "UBL-Invoice-2.1.xsd"));
        if (includeDianExtensions)
        {
            schemas.Add(DianNamespace, Path.Combine(root, "DIAN_UBL_Structures.xsd"));
        }
        schemas.Compile();
        return schemas;
    }

    private static List<string> Validate(XDocument document, bool includeDianExtensions = true)
    {
        var errors = new List<string>();
        document.Validate(LoadSchemas(includeDianExtensions), (_, args) => errors.Add(args.Message));
        return errors;
    }

    private static void AssertSchemaValid(XDocument document, bool includeDianExtensions = true)
    {
        var errors = Validate(document, includeDianExtensions);
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
    }
}
