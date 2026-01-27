namespace Gateway.API.Tests.Services.Fhir;

using Gateway.API.Contracts.Fhir;
using Gateway.API.Services.Fhir;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Task = System.Threading.Tasks.Task;

public class FhirSerializerTests
{
    private readonly IFhirSerializer _serializer;
    private readonly ILogger<FhirSerializer> _logger;

    public FhirSerializerTests()
    {
        _logger = Substitute.For<ILogger<FhirSerializer>>();
        _serializer = new FhirSerializer(_logger);
    }

    [Test]
    public async Task Serialize_Patient_ProducesValidJson()
    {
        // Arrange
        var patient = new Patient
        {
            Id = "123",
            Name = { new HumanName { Family = "Doe", Given = new[] { "John" } } }
        };

        // Act
        var json = _serializer.Serialize(patient);

        // Assert
        await Assert.That(json).Contains("\"resourceType\":\"Patient\"");
        await Assert.That(json).Contains("\"id\":\"123\"");
        await Assert.That(json).Contains("\"family\":\"Doe\"");
    }

    [Test]
    public async Task Serialize_NullResource_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => _serializer.Serialize<Patient>(null!));
        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task Deserialize_ValidPatientJson_ReturnsPatient()
    {
        // Arrange
        var json = """
        {
            "resourceType": "Patient",
            "id": "456",
            "name": [{"family": "Smith", "given": ["Jane"]}]
        }
        """;

        // Act
        var patient = _serializer.Deserialize<Patient>(json);

        // Assert
        await Assert.That(patient).IsNotNull();
        await Assert.That(patient!.Id).IsEqualTo("456");
        await Assert.That(patient.Name[0].Family).IsEqualTo("Smith");
    }

    [Test]
    public async Task Deserialize_InvalidJson_ReturnsNull()
    {
        var json = "{ invalid json }";
        var result = _serializer.Deserialize<Patient>(json);
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Deserialize_EmptyString_ReturnsNull()
    {
        var result = _serializer.Deserialize<Patient>("");
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Deserialize_NullString_ReturnsNull()
    {
        var result = _serializer.Deserialize<Patient>(null!);
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task DeserializeBundle_ValidBundle_ReturnsBundle()
    {
        // Arrange
        var json = """
        {
            "resourceType": "Bundle",
            "type": "searchset",
            "entry": [
                {
                    "resource": {
                        "resourceType": "Patient",
                        "id": "p1"
                    }
                },
                {
                    "resource": {
                        "resourceType": "Patient",
                        "id": "p2"
                    }
                }
            ]
        }
        """;

        // Act
        var bundle = _serializer.DeserializeBundle(json);

        // Assert
        await Assert.That(bundle).IsNotNull();
        await Assert.That(bundle!.Entry.Count).IsEqualTo(2);
    }

    [Test]
    public async Task DeserializeBundle_InvalidJson_ReturnsNull()
    {
        var result = _serializer.DeserializeBundle("not valid json");
        await Assert.That(result).IsNull();
    }
}
