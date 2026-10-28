using System.CodeDom;
using XmlSchemaClassGenerator;

if (args.Length != 1)
{
    Console.Error.WriteLine("Uso: IFC.Facturacion.XmlGenerator <raiz-del-proyecto>");
    return 1;
}

var root = Path.GetFullPath(args[0]);
var schemas = Path.Combine(root, "src", "IFC.Facturacion.Infrastructure", "Dian", "Schemas", "V1_9");
var output = Path.Combine(root, "src", "IFC.Facturacion.Infrastructure", "Dian", "Xml", "Generated");
const string prefix = "IFC.Facturacion.Infrastructure.Dian.Xml";
const string ubl = "urn:oasis:names:specification:ubl:schema:xsd:";
var namespaces = new NamespaceProvider
{
    GenerateNamespace = key => throw new InvalidOperationException($"Namespace XSD sin mapeo: {key}")
};

foreach (var (xml, name) in new (string, string)[]
{
    (ubl + "Invoice-2", "Invoice"),
    (ubl + "CommonAggregateComponents-2", "CommonAggregate"),
    (ubl + "CommonBasicComponents-2", "CommonBasic"),
    (ubl + "CommonExtensionComponents-2", "CommonExtension"),
    (ubl + "CommonSignatureComponents-2", "CommonSignature"),
    (ubl + "SignatureAggregateComponents-2", "SignatureAggregate"),
    (ubl + "SignatureBasicComponents-2", "SignatureBasic"),
    (ubl + "QualifiedDataTypes-2", "Qualified"),
    (ubl + "UnqualifiedDataTypes-2", "Unqualified"),
    ("urn:un:unece:uncefact:data:specification:CoreComponentTypeSchemaModule:2", "Core"),
    ("dian:gov:co:facturaelectronica:Structures-2-1", "DianExtensions"),
    ("http://www.w3.org/2000/09/xmldsig#", "XmlDsig"),
    ("http://uri.etsi.org/01903/v1.3.2#", "Xades132"),
    ("http://uri.etsi.org/01903/v1.4.1#", "Xades141")
})
{
    namespaces.Add(new NamespaceKey(xml), $"{prefix}.{name}");
}

var lexicalDateTimeTypes = 0;
var generator = new Generator
{
    OutputFolder = output,
    NamespaceProvider = namespaces,
    EmitOrder = true,
    GenerateNullables = true,
    GenerateInterfaces = false,
    DisableComments = true,
    GenerateDescriptionAttribute = false,
    GenerateCommandLineArgumentsComment = false,
    NetCoreSpecificCode = true,
    EnumAsString = true,
    Log = Console.WriteLine,
    TypeVisitor = (type, _) =>
    {
        foreach (var property in type.Members.OfType<CodeMemberField>()
                     .Where(property => property.Name.StartsWith("Value ", StringComparison.Ordinal) &&
                                        property.Type.BaseType == typeof(DateTime).FullName))
        {
            var textAttribute = property.CustomAttributes.Cast<CodeAttributeDeclaration>()
                .SingleOrDefault(attribute => attribute.Name == "System.Xml.Serialization.XmlTextAttribute");
            if (textAttribute is null)
            {
                continue;
            }

            // Preserve the XML lexical date/time, especially the offset used in CUFE inputs.
            property.Type = new CodeTypeReference(typeof(string));
            foreach (var argument in textAttribute.Arguments.Cast<CodeAttributeArgument>()
                         .Where(argument => argument.Name == "DataType").ToArray())
            {
                textAttribute.Arguments.Remove(argument);
            }
            lexicalDateTimeTypes++;
        }
    }
};

generator.Generate(new[]
{
    Path.Combine(schemas, "maindoc", "UBL-Invoice-2.1.xsd"),
    Path.Combine(schemas, "maindoc", "DIAN_UBL_Structures.xsd")
});

if (lexicalDateTimeTypes != 3)
{
    throw new InvalidOperationException(
        $"Se esperaban 3 tipos de fecha/hora lexicos; se generaron {lexicalDateTimeTypes}.");
}

return 0;
