using CodeCoverageReporter.Cobertura.Models;
using CodeCoverageReporter.Cobertura.Parsing;

namespace CodeCoverageReporter.Cobertura.Tests.Parsing;

public sealed class CoberturaParserTests
{
    private readonly CoberturaParser _parser = new();

    [Fact]
    public void Parse_ValidXml_ReturnsCoverageReport()
    {
        // Arrange
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage line-rate="0.75" branch-rate="0.5" version="1.9" timestamp="1234567890">
              <sources>
                <source>/src/</source>
              </sources>
              <packages>
                <package name="TestPackage" line-rate="0.75" branch-rate="0.5" complexity="10">
                  <classes>
                    <class name="TestClass" filename="Test.cs" line-rate="0.75" branch-rate="0.5" complexity="5">
                      <methods>
                        <method name="TestMethod" signature="()" line-rate="0.75" branch-rate="0.5" complexity="2">
                          <lines>
                            <line number="10" hits="5" branch="false" />
                            <line number="11" hits="0" branch="false" />
                          </lines>
                        </method>
                      </methods>
                      <lines>
                        <line number="10" hits="5" branch="false" />
                        <line number="11" hits="0" branch="false" />
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert
        Assert.NotNull(report);
        Assert.Equal(0.75, report.LineRate);
        Assert.Equal(0.5, report.BranchRate);
        Assert.Equal("1.9", report.Version);
        Assert.Equal(1234567890, report.Timestamp);
        Assert.Single(report.Sources);
        Assert.Equal("/src/", report.Sources[0]);
        Assert.Single(report.Packages);
    }

    [Fact]
    public void Parse_PackageHierarchy_ParsesCorrectly()
    {
        // Arrange
        const string xml = """
            <?xml version="1.0"?>
            <coverage line-rate="0.8" branch-rate="0.6" version="1.9" timestamp="0">
              <packages>
                <package name="MyApp.Services" line-rate="0.8" branch-rate="0.6" complexity="15">
                  <classes>
                    <class name="MyApp.Services.UserService" filename="UserService.cs" line-rate="0.9" branch-rate="0.7" complexity="8">
                      <methods>
                        <method name="GetUser" signature="(System.Int32)" line-rate="1.0" branch-rate="1.0" complexity="1">
                          <lines>
                            <line number="20" hits="10" branch="false" />
                            <line number="21" hits="10" branch="false" />
                          </lines>
                        </method>
                      </methods>
                      <lines>
                        <line number="20" hits="10" branch="false" />
                        <line number="21" hits="10" branch="false" />
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert
        var package = Assert.Single(report.Packages);
        Assert.Equal("MyApp.Services", package.Name);
        Assert.Equal(0.8, package.LineRate);
        Assert.Equal(15, package.Complexity);

        var classItem = Assert.Single(package.Classes);
        Assert.Equal("MyApp.Services.UserService", classItem.Name);
        Assert.Equal("UserService.cs", classItem.FilePath);

        var method = Assert.Single(classItem.Methods);
        Assert.Equal("GetUser", method.Name);
        Assert.Equal("(System.Int32)", method.Signature);
        Assert.Equal(2, method.Lines.Count);
    }

    [Fact]
    public void Parse_BooleanAttributesCaseInsensitive_ParsesCorrectly()
    {
        // Arrange
        const string xml = """
            <?xml version="1.0"?>
            <coverage line-rate="1.0" branch-rate="1.0" version="1.9" timestamp="0">
              <packages>
                <package name="Test" line-rate="1.0" branch-rate="1.0" complexity="1">
                  <classes>
                    <class name="TestClass" filename="Test.cs" line-rate="1.0" branch-rate="1.0" complexity="1">
                      <methods>
                        <method name="TestMethod" signature="()" line-rate="1.0" branch-rate="1.0" complexity="1">
                          <lines>
                            <line number="1" hits="1" branch="True" />
                            <line number="2" hits="1" branch="true" />
                            <line number="3" hits="1" branch="TRUE" />
                            <line number="4" hits="1" branch="False" />
                            <line number="5" hits="1" branch="false" />
                          </lines>
                        </method>
                      </methods>
                      <lines>
                        <line number="1" hits="1" branch="True" />
                        <line number="2" hits="1" branch="true" />
                        <line number="3" hits="1" branch="TRUE" />
                        <line number="4" hits="1" branch="False" />
                        <line number="5" hits="1" branch="false" />
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert
        var lines = report.Packages[0].Classes[0].Methods[0].Lines;
        Assert.True(lines[0].IsBranch);  // True
        Assert.True(lines[1].IsBranch);  // true
        Assert.True(lines[2].IsBranch);  // TRUE
        Assert.False(lines[3].IsBranch); // False
        Assert.False(lines[4].IsBranch); // false
    }

    [Fact]
    public void Parse_BranchConditions_ParsesCorrectly()
    {
        // Arrange
        const string xml = """
            <?xml version="1.0"?>
            <coverage line-rate="1.0" branch-rate="0.5" version="1.9" timestamp="0">
              <packages>
                <package name="Test" line-rate="1.0" branch-rate="0.5" complexity="2">
                  <classes>
                    <class name="TestClass" filename="Test.cs" line-rate="1.0" branch-rate="0.5" complexity="2">
                      <methods>
                        <method name="TestMethod" signature="()" line-rate="1.0" branch-rate="0.5" complexity="2">
                          <lines>
                            <line number="10" hits="5" branch="true" condition-coverage="50% (1/2)">
                              <conditions>
                                <condition number="0" type="jump" coverage="100%" />
                                <condition number="1" type="jump" coverage="0%" />
                              </conditions>
                            </line>
                          </lines>
                        </method>
                      </methods>
                      <lines>
                        <line number="10" hits="5" branch="true" condition-coverage="50% (1/2)">
                          <conditions>
                            <condition number="0" type="jump" coverage="100%" />
                            <condition number="1" type="jump" coverage="0%" />
                          </conditions>
                        </line>
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert
        var line = report.Packages[0].Classes[0].Methods[0].Lines[0];
        Assert.True(line.IsBranch);
        Assert.Equal("50% (1/2)", line.ConditionCoverage);
        Assert.Equal(2, line.Conditions.Count);

        Assert.Equal(0, line.Conditions[0].Number);
        Assert.Equal("jump", line.Conditions[0].Type);
        Assert.Equal("100%", line.Conditions[0].Coverage);

        Assert.Equal(1, line.Conditions[1].Number);
        Assert.Equal("jump", line.Conditions[1].Type);
        Assert.Equal("0%", line.Conditions[1].Coverage);
    }

    [Fact]
    public void Parse_BranchWithoutConditions_ParsesAsEmptyList()
    {
        // Arrange
        const string xml = """
            <?xml version="1.0"?>
            <coverage line-rate="1.0" branch-rate="1.0" version="1.9" timestamp="0">
              <packages>
                <package name="Test" line-rate="1.0" branch-rate="1.0" complexity="1">
                  <classes>
                    <class name="TestClass" filename="Test.cs" line-rate="1.0" branch-rate="1.0" complexity="1">
                      <methods>
                        <method name="TestMethod" signature="()" line-rate="1.0" branch-rate="1.0" complexity="1">
                          <lines>
                            <line number="10" hits="5" branch="true" />
                          </lines>
                        </method>
                      </methods>
                      <lines>
                        <line number="10" hits="5" branch="true" />
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert
        var line = report.Packages[0].Classes[0].Methods[0].Lines[0];
        Assert.True(line.IsBranch);
        Assert.Null(line.ConditionCoverage);
        Assert.Empty(line.Conditions);
    }

    [Fact]
    public void Parse_ClassLinesNotInMethods_ParsedSeparately()
    {
        // Arrange - class has lines 10, 11, 12 but method only has line 10
        const string xml = """
            <?xml version="1.0"?>
            <coverage line-rate="1.0" branch-rate="1.0" version="1.9" timestamp="0">
              <packages>
                <package name="Test" line-rate="1.0" branch-rate="1.0" complexity="1">
                  <classes>
                    <class name="TestClass" filename="Test.cs" line-rate="1.0" branch-rate="1.0" complexity="1">
                      <methods>
                        <method name="TestMethod" signature="()" line-rate="1.0" branch-rate="1.0" complexity="1">
                          <lines>
                            <line number="10" hits="5" branch="false" />
                          </lines>
                        </method>
                      </methods>
                      <lines>
                        <line number="10" hits="5" branch="false" />
                        <line number="11" hits="3" branch="false" />
                        <line number="12" hits="0" branch="false" />
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert
        var classItem = report.Packages[0].Classes[0];

        // Method should have 1 line
        Assert.Single(classItem.Methods[0].Lines);
        Assert.Equal(10, classItem.Methods[0].Lines[0].Number);

        // ClassLines should have 2 lines (11 and 12, not in any method)
        Assert.Equal(2, classItem.ClassLines.Count);
        Assert.Equal(11, classItem.ClassLines[0].Number);
        Assert.Equal(12, classItem.ClassLines[1].Number);

        // Verify scope
        Assert.Equal(LineScope.Method, classItem.Methods[0].Lines[0].Scope);
        Assert.Equal(LineScope.Class, classItem.ClassLines[0].Scope);
    }

    [Fact]
    public void Parse_TotalAndCoveredLinesCalculated()
    {
        // Arrange
        const string xml = """
            <?xml version="1.0"?>
            <coverage line-rate="0.5" branch-rate="0" version="1.9" timestamp="0">
              <packages>
                <package name="Test" line-rate="0.5" branch-rate="0" complexity="1">
                  <classes>
                    <class name="TestClass" filename="Test.cs" line-rate="0.5" branch-rate="0" complexity="1">
                      <methods>
                        <method name="TestMethod" signature="()" line-rate="0.5" branch-rate="0" complexity="1">
                          <lines>
                            <line number="10" hits="5" branch="false" />
                            <line number="11" hits="0" branch="false" />
                          </lines>
                        </method>
                      </methods>
                      <lines>
                        <line number="10" hits="5" branch="false" />
                        <line number="11" hits="0" branch="false" />
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert
        var method = report.Packages[0].Classes[0].Methods[0];
        Assert.Equal(2, method.TotalLines);
        Assert.Equal(1, method.CoveredLines);

        var classItem = report.Packages[0].Classes[0];
        Assert.Equal(2, classItem.TotalLines);
        Assert.Equal(1, classItem.CoveredLines);

        var package = report.Packages[0];
        Assert.Equal(2, package.TotalLines);
        Assert.Equal(1, package.CoveredLines);

        Assert.Equal(2, report.TotalLines);
        Assert.Equal(1, report.CoveredLines);
    }

    [Fact]
    public void Parse_MissingSources_ReturnsEmptyList()
    {
        // Arrange
        const string xml = """
            <?xml version="1.0"?>
            <coverage line-rate="1.0" branch-rate="1.0" version="1.9" timestamp="0">
              <packages>
              </packages>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert
        Assert.Empty(report.Sources);
    }

    [Fact]
    public void Parse_MissingPackages_ReturnsEmptyList()
    {
        // Arrange
        const string xml = """
            <?xml version="1.0"?>
            <coverage line-rate="1.0" branch-rate="1.0" version="1.9" timestamp="0">
              <sources>
                <source>/src/</source>
              </sources>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert
        Assert.Empty(report.Packages);
    }

    [Fact]
    public void Parse_MissingOptionalAttributes_UsesDefaults()
    {
        // Arrange
        const string xml = """
            <?xml version="1.0"?>
            <coverage version="1.9" timestamp="0">
              <packages>
                <package name="Test">
                  <classes>
                    <class name="TestClass">
                      <methods>
                        <method name="TestMethod">
                          <lines>
                            <line number="1" hits="1" />
                          </lines>
                        </method>
                      </methods>
                      <lines>
                        <line number="1" hits="1" />
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert
        Assert.Equal(0.0, report.LineRate);
        Assert.Equal(0.0, report.BranchRate);

        var package = report.Packages[0];
        Assert.Equal(0.0, package.LineRate);
        Assert.Equal(0, package.Complexity);

        var classItem = package.Classes[0];
        Assert.Null(classItem.FilePath);
    }

    [Fact]
    public void Parse_InvalidCoberturaXml_ThrowsInvalidOperationException()
    {
        // Arrange - valid XML but not Cobertura format (missing coverage element)
        const string invalidCoberturaXml = "<not-coverage></not-coverage>";

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _parser.Parse(invalidCoberturaXml));
    }

    [Fact]
    public void Parse_MalformedXml_ThrowsException()
    {
        // Arrange
        const string malformedXml = "<coverage><unclosed-tag>";

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => _parser.Parse(malformedXml));
    }

    [Fact]
    public void Parse_FromStream_WorksCorrectly()
    {
        // Arrange
        const string xml = """
            <?xml version="1.0"?>
            <coverage line-rate="0.8" branch-rate="0.6" version="1.9" timestamp="12345">
              <packages>
              </packages>
            </coverage>
            """;
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));

        // Act
        var report = _parser.Parse(stream);

        // Assert
        Assert.Equal(0.8, report.LineRate);
        Assert.Equal(0.6, report.BranchRate);
        Assert.Equal(12345, report.Timestamp);
    }

    [Fact]
    public void Parse_RealCoberturaFile_ParsesSuccessfully()
    {
        // Arrange - find the code_coverage directory by walking up from test project location
        // AppContext.BaseDirectory is typically bin/Debug/net10.0, need to walk up to repo root
        var currentDir = AppContext.BaseDirectory;
        string[] coberturaFiles = [];

        // Walk up until we find a code_coverage directory with XML files or hit the root
        var searchDir = currentDir;
        while (!string.IsNullOrEmpty(searchDir))
        {
            var candidatePath = Path.Combine(searchDir, "code_coverage");
            if (Directory.Exists(candidatePath))
            {
                coberturaFiles = Directory.GetFiles(candidatePath, "coverage.cobertura.xml", SearchOption.AllDirectories);
                if (coberturaFiles.Length > 0)
                {
                    break;
                }
            }
            searchDir = Path.GetDirectoryName(searchDir);
        }

        if (coberturaFiles.Length == 0)
        {
            // No coverage files to test - this is expected in CI/fresh environments
            return;
        }

        foreach (var filePath in coberturaFiles)
        {
            // Act
            using var stream = File.OpenRead(filePath);
            var report = _parser.Parse(stream);

            // Assert basic invariants
            Assert.NotNull(report);
            Assert.NotNull(report.Packages);
            Assert.NotNull(report.Sources);
            Assert.True(report.LineRate >= 0 && report.LineRate <= 1);
            Assert.True(report.BranchRate >= 0 && report.BranchRate <= 1);

            foreach (var package in report.Packages)
            {
                Assert.NotNull(package.Name);
                Assert.NotNull(package.Classes);

                foreach (var classItem in package.Classes)
                {
                    Assert.NotNull(classItem.Name);
                    Assert.NotNull(classItem.Methods);
                    Assert.NotNull(classItem.ClassLines);

                    foreach (var method in classItem.Methods)
                    {
                        Assert.NotNull(method.Name);
                        Assert.NotNull(method.Lines);
                    }
                }
            }
        }
    }

    [Fact]
    public void Parse_LinesScopeCorrect_MethodVsClass()
    {
        // Arrange
        const string xml = """
            <?xml version="1.0"?>
            <coverage line-rate="1.0" branch-rate="1.0" version="1.9" timestamp="0">
              <packages>
                <package name="Test" line-rate="1.0" branch-rate="1.0" complexity="1">
                  <classes>
                    <class name="TestClass" filename="Test.cs" line-rate="1.0" branch-rate="1.0" complexity="1">
                      <methods>
                        <method name="MethodA" signature="()" line-rate="1.0" branch-rate="1.0" complexity="1">
                          <lines>
                            <line number="10" hits="1" branch="false" />
                          </lines>
                        </method>
                      </methods>
                      <lines>
                        <line number="10" hits="1" branch="false" />
                        <line number="5" hits="1" branch="false" />
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert
        var classItem = report.Packages[0].Classes[0];

        // Method line should have Method scope
        Assert.All(classItem.Methods[0].Lines, l => Assert.Equal(LineScope.Method, l.Scope));

        // Class lines (not in method) should have Class scope
        Assert.All(classItem.ClassLines, l => Assert.Equal(LineScope.Class, l.Scope));

        // Line 5 is in ClassLines (not in any method)
        Assert.Contains(classItem.ClassLines, l => l.Number == 5);
    }

    [Fact]
    public void Parse_FilePathPropagatedToLines()
    {
        // Arrange
        const string xml = """
            <?xml version="1.0"?>
            <coverage line-rate="1.0" branch-rate="1.0" version="1.9" timestamp="0">
              <packages>
                <package name="Test" line-rate="1.0" branch-rate="1.0" complexity="1">
                  <classes>
                    <class name="TestClass" filename="src/Test.cs" line-rate="1.0" branch-rate="1.0" complexity="1">
                      <methods>
                        <method name="TestMethod" signature="()" line-rate="1.0" branch-rate="1.0" complexity="1">
                          <lines>
                            <line number="10" hits="1" branch="false" />
                          </lines>
                        </method>
                      </methods>
                      <lines>
                        <line number="10" hits="1" branch="false" />
                        <line number="11" hits="1" branch="false" />
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert
        var classItem = report.Packages[0].Classes[0];

        // Method lines should have file path
        Assert.All(classItem.Methods[0].Lines, l => Assert.Equal("src/Test.cs", l.FilePath));

        // Class lines should also have file path
        Assert.All(classItem.ClassLines, l => Assert.Equal("src/Test.cs", l.FilePath));
    }

    [Fact]
    public void Parse_EmptyPackage_HandledGracefully()
    {
        // Arrange
        const string xml = """
            <?xml version="1.0"?>
            <coverage line-rate="0" branch-rate="0" version="1.9" timestamp="0">
              <packages>
                <package name="EmptyPackage" line-rate="0" branch-rate="0" complexity="0">
                  <classes>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert
        var package = Assert.Single(report.Packages);
        Assert.Equal("EmptyPackage", package.Name);
        Assert.Empty(package.Classes);
        Assert.Equal(0, package.TotalLines);
        Assert.Equal(0, package.CoveredLines);
    }

    [Fact]
    public void Parse_EmptyClass_HandledGracefully()
    {
        // Arrange
        const string xml = """
            <?xml version="1.0"?>
            <coverage line-rate="0" branch-rate="0" version="1.9" timestamp="0">
              <packages>
                <package name="Test" line-rate="0" branch-rate="0" complexity="0">
                  <classes>
                    <class name="EmptyClass" filename="Empty.cs" line-rate="0" branch-rate="0" complexity="0">
                      <methods>
                      </methods>
                      <lines>
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert
        var classItem = Assert.Single(report.Packages[0].Classes);
        Assert.Equal("EmptyClass", classItem.Name);
        Assert.Empty(classItem.Methods);
        Assert.Empty(classItem.ClassLines);
        Assert.Equal(0, classItem.TotalLines);
        Assert.Equal(0, classItem.CoveredLines);
    }

    [Fact]
    public void Parse_PackageWithoutClassesElement_ReturnsEmptyClassesList()
    {
        // Arrange - package has no <classes> child element at all
        const string xml = """
            <?xml version="1.0"?>
            <coverage line-rate="0" branch-rate="0" version="1.9" timestamp="0">
              <packages>
                <package name="EmptyPackage" line-rate="0" branch-rate="0" complexity="0">
                </package>
              </packages>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert
        var package = Assert.Single(report.Packages);
        Assert.Equal("EmptyPackage", package.Name);
        Assert.Empty(package.Classes);
    }

    [Fact]
    public void Parse_ClassWithoutMethodsElement_ReturnsEmptyMethodsList()
    {
        // Arrange - class has no <methods> child element at all
        const string xml = """
            <?xml version="1.0"?>
            <coverage line-rate="0" branch-rate="0" version="1.9" timestamp="0">
              <packages>
                <package name="Test" line-rate="0" branch-rate="0" complexity="0">
                  <classes>
                    <class name="NoMethodsClass" filename="Test.cs" line-rate="0" branch-rate="0" complexity="0">
                      <lines>
                        <line number="1" hits="1" branch="false" />
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert
        var classItem = Assert.Single(report.Packages[0].Classes);
        Assert.Equal("NoMethodsClass", classItem.Name);
        Assert.Empty(classItem.Methods);
        Assert.Single(classItem.ClassLines); // The line should be in ClassLines
    }

    [Fact]
    public void Parse_MethodWithoutLinesElement_ReturnsEmptyLinesList()
    {
        // Arrange - method has no <lines> child element at all
        const string xml = """
            <?xml version="1.0"?>
            <coverage line-rate="0" branch-rate="0" version="1.9" timestamp="0">
              <packages>
                <package name="Test" line-rate="0" branch-rate="0" complexity="0">
                  <classes>
                    <class name="TestClass" filename="Test.cs" line-rate="0" branch-rate="0" complexity="0">
                      <methods>
                        <method name="NoLinesMethod" signature="()" line-rate="0" branch-rate="0" complexity="0">
                        </method>
                      </methods>
                      <lines>
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert
        var method = Assert.Single(report.Packages[0].Classes[0].Methods);
        Assert.Equal("NoLinesMethod", method.Name);
        Assert.Empty(method.Lines);
        Assert.Equal(0, method.TotalLines);
        Assert.Equal(0, method.CoveredLines);
    }

    [Fact]
    public void Parse_ClassWithoutLinesElement_ReturnsEmptyClassLinesList()
    {
        // Arrange - class has methods but no class-level <lines> element
        const string xml = """
            <?xml version="1.0"?>
            <coverage line-rate="0" branch-rate="0" version="1.9" timestamp="0">
              <packages>
                <package name="Test" line-rate="0" branch-rate="0" complexity="0">
                  <classes>
                    <class name="TestClass" filename="Test.cs" line-rate="0" branch-rate="0" complexity="0">
                      <methods>
                        <method name="TestMethod" signature="()" line-rate="1.0" branch-rate="0" complexity="1">
                          <lines>
                            <line number="10" hits="1" branch="false" />
                          </lines>
                        </method>
                      </methods>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert
        var classItem = Assert.Single(report.Packages[0].Classes);
        Assert.Single(classItem.Methods);
        Assert.Empty(classItem.ClassLines); // No class-level lines element
        Assert.Equal(1, classItem.TotalLines); // Only method lines counted
    }

    [Fact]
    public void Parse_InvalidDoubleAttribute_ReturnsDefaultValue()
    {
        // Arrange - invalid non-numeric values for line-rate and branch-rate
        const string xml = """
            <?xml version="1.0"?>
            <coverage line-rate="invalid" branch-rate="not-a-number" version="1.9" timestamp="0">
              <packages>
              </packages>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert - should fall back to default 0.0 for unparseable doubles
        Assert.Equal(0.0, report.LineRate);
        Assert.Equal(0.0, report.BranchRate);
    }

    [Fact]
    public void Parse_InvalidIntAttribute_ReturnsDefaultValue()
    {
        // Arrange - invalid non-numeric values for complexity and line numbers
        const string xml = """
            <?xml version="1.0"?>
            <coverage line-rate="0.5" branch-rate="0.5" version="1.9" timestamp="0">
              <packages>
                <package name="Test" line-rate="0.5" branch-rate="0.5" complexity="invalid">
                  <classes>
                    <class name="TestClass" filename="Test.cs" line-rate="0.5" branch-rate="0.5" complexity="not-a-number">
                      <methods>
                        <method name="TestMethod" signature="()" line-rate="0.5" branch-rate="0.5" complexity="bad">
                          <lines>
                            <line number="abc" hits="xyz" branch="false" />
                          </lines>
                        </method>
                      </methods>
                      <lines>
                        <line number="abc" hits="xyz" branch="false" />
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert - should fall back to default 0 for unparseable ints
        var package = Assert.Single(report.Packages);
        Assert.Equal(0, package.Complexity);

        var classItem = Assert.Single(package.Classes);
        Assert.Equal(0, classItem.Complexity);

        var method = Assert.Single(classItem.Methods);
        Assert.Equal(0, method.Complexity);

        var line = Assert.Single(method.Lines);
        Assert.Equal(0, line.Number);
        Assert.Equal(0, line.Hits);
    }

    [Fact]
    public void Parse_InvalidLongAttribute_ReturnsDefaultValue()
    {
        // Arrange - invalid non-numeric timestamp value
        const string xml = """
            <?xml version="1.0"?>
            <coverage line-rate="0.5" branch-rate="0.5" version="1.9" timestamp="invalid-timestamp">
              <packages>
              </packages>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert - should fall back to default 0 for unparseable long
        Assert.Equal(0, report.Timestamp);
    }

    [Fact]
    public void Parse_EmptyLongAttribute_ReturnsDefaultValue()
    {
        // Arrange - empty timestamp value
        const string xml = """
            <?xml version="1.0"?>
            <coverage line-rate="0.5" branch-rate="0.5" version="1.9" timestamp="">
              <packages>
              </packages>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert - should fall back to default 0 for empty value
        Assert.Equal(0, report.Timestamp);
    }

    [Fact]
    public void Parse_MissingTimestampAttribute_ReturnsDefaultValue()
    {
        // Arrange - no timestamp attribute at all
        const string xml = """
            <?xml version="1.0"?>
            <coverage line-rate="0.5" branch-rate="0.5" version="1.9">
              <packages>
              </packages>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert - should fall back to default 0 for missing attribute
        Assert.Equal(0, report.Timestamp);
    }

    [Fact]
    public void Parse_ConditionWithMissingAttributes_UsesDefaults()
    {
        // Arrange - condition element without number, type, or coverage attributes
        const string xml = """
            <?xml version="1.0"?>
            <coverage line-rate="1.0" branch-rate="0.5" version="1.9" timestamp="0">
              <packages>
                <package name="Test" line-rate="1.0" branch-rate="0.5" complexity="1">
                  <classes>
                    <class name="TestClass" filename="Test.cs" line-rate="1.0" branch-rate="0.5" complexity="1">
                      <methods>
                        <method name="TestMethod" signature="()" line-rate="1.0" branch-rate="0.5" complexity="1">
                          <lines>
                            <line number="10" hits="1" branch="true">
                              <conditions>
                                <condition />
                              </conditions>
                            </line>
                          </lines>
                        </method>
                      </methods>
                      <lines>
                        <line number="10" hits="1" branch="true">
                          <conditions>
                            <condition />
                          </conditions>
                        </line>
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert - condition should use default values
        var line = report.Packages[0].Classes[0].Methods[0].Lines[0];
        var condition = Assert.Single(line.Conditions);
        Assert.Equal(0, condition.Number);
        Assert.Equal("", condition.Type);
        Assert.Equal("", condition.Coverage);
    }

    [Fact]
    public void Parse_AbsoluteFilePath_ReturnsAsIs()
    {
        // Arrange - class with absolute file path (rooted path)
        const string xml = """
            <?xml version="1.0"?>
            <coverage line-rate="1.0" branch-rate="1.0" version="1.9" timestamp="0">
              <sources>
                <source>/some/source/dir</source>
              </sources>
              <packages>
                <package name="Test" line-rate="1.0" branch-rate="1.0" complexity="1">
                  <classes>
                    <class name="TestClass" filename="/absolute/path/to/Test.cs" line-rate="1.0" branch-rate="1.0" complexity="1">
                      <methods>
                        <method name="TestMethod" signature="()" line-rate="1.0" branch-rate="1.0" complexity="1">
                          <lines>
                            <line number="10" hits="1" branch="false" />
                          </lines>
                        </method>
                      </methods>
                      <lines>
                        <line number="10" hits="1" branch="false" />
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert - absolute path should be returned as-is, not resolved against sources
        var classItem = Assert.Single(report.Packages[0].Classes);
        Assert.Equal("/absolute/path/to/Test.cs", classItem.FilePath);
    }

    [Fact]
    public void Parse_RelativeFilePath_WithNonExistentSourceFile_ReturnsRawFilename()
    {
        // Arrange - relative path with sources that don't contain the file
        const string xml = """
            <?xml version="1.0"?>
            <coverage line-rate="1.0" branch-rate="1.0" version="1.9" timestamp="0">
              <sources>
                <source>/nonexistent/source/path</source>
                <source>/another/nonexistent/path</source>
              </sources>
              <packages>
                <package name="Test" line-rate="1.0" branch-rate="1.0" complexity="1">
                  <classes>
                    <class name="TestClass" filename="relative/path/Test.cs" line-rate="1.0" branch-rate="1.0" complexity="1">
                      <methods>
                        <method name="TestMethod" signature="()" line-rate="1.0" branch-rate="1.0" complexity="1">
                          <lines>
                            <line number="10" hits="1" branch="false" />
                          </lines>
                        </method>
                      </methods>
                      <lines>
                        <line number="10" hits="1" branch="false" />
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert - should fall back to raw filename when file doesn't exist in any source
        var classItem = Assert.Single(report.Packages[0].Classes);
        Assert.Equal("relative/path/Test.cs", classItem.FilePath);
    }

    [Fact]
    public void Parse_RelativeFilePath_WithExistingSourceFile_ReturnsResolvedPath()
    {
        // Arrange - use the actual test project directory as a source
        var testProjectDir = Path.GetDirectoryName(typeof(CoberturaParserTests).Assembly.Location)!;
        // Walk up to find the test project source directory
        var searchDir = testProjectDir;
        while (searchDir != null && !File.Exists(Path.Combine(searchDir, "CodeCoverageReporter.Cobertura.Tests.csproj")))
        {
            searchDir = Path.GetDirectoryName(searchDir);
        }

        // If we can't find test project dir, use a temp file approach
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var testFile = Path.Combine(tempDir, "TestFile.cs");
        File.WriteAllText(testFile, "// test");

        try
        {
            var xml = $"""
                <?xml version="1.0"?>
                <coverage line-rate="1.0" branch-rate="1.0" version="1.9" timestamp="0">
                  <sources>
                    <source>{tempDir}</source>
                  </sources>
                  <packages>
                    <package name="Test" line-rate="1.0" branch-rate="1.0" complexity="1">
                      <classes>
                        <class name="TestClass" filename="TestFile.cs" line-rate="1.0" branch-rate="1.0" complexity="1">
                          <methods>
                            <method name="TestMethod" signature="()" line-rate="1.0" branch-rate="1.0" complexity="1">
                              <lines>
                                <line number="10" hits="1" branch="false" />
                              </lines>
                            </method>
                          </methods>
                          <lines>
                            <line number="10" hits="1" branch="false" />
                          </lines>
                        </class>
                      </classes>
                    </package>
                  </packages>
                </coverage>
                """;

            // Act
            var report = _parser.Parse(xml);

            // Assert - should resolve to full path since file exists
            var classItem = Assert.Single(report.Packages[0].Classes);
            Assert.Equal(testFile, classItem.FilePath);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Parse_WithVersionAttribute_ParsesVersionCorrectly()
    {
        // Arrange - explicitly test the version attribute path (non-null case)
        const string xml = """
            <?xml version="1.0"?>
            <coverage line-rate="1.0" branch-rate="1.0" version="2.0.1" timestamp="0">
              <packages>
              </packages>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert
        Assert.Equal("2.0.1", report.Version);
    }

    [Fact]
    public void Parse_WithoutVersionAttribute_ReturnsEmptyString()
    {
        // Arrange - test missing version attribute (null coalescing to "")
        const string xml = """
            <?xml version="1.0"?>
            <coverage line-rate="1.0" branch-rate="1.0" timestamp="0">
              <packages>
              </packages>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert
        Assert.Equal("", report.Version);
    }

    [Fact]
    public void Parse_PackageWithMissingNameAttribute_ReturnsEmptyString()
    {
        // Arrange - package without name attribute
        const string xml = """
            <?xml version="1.0"?>
            <coverage line-rate="1.0" branch-rate="1.0" version="1.9" timestamp="0">
              <packages>
                <package line-rate="1.0" branch-rate="1.0" complexity="0">
                  <classes>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert
        var package = Assert.Single(report.Packages);
        Assert.Equal("", package.Name);
    }

    [Fact]
    public void Parse_ClassWithMissingNameAttribute_ReturnsEmptyString()
    {
        // Arrange - class without name attribute
        const string xml = """
            <?xml version="1.0"?>
            <coverage line-rate="1.0" branch-rate="1.0" version="1.9" timestamp="0">
              <packages>
                <package name="Test" line-rate="1.0" branch-rate="1.0" complexity="0">
                  <classes>
                    <class filename="Test.cs" line-rate="1.0" branch-rate="1.0" complexity="0">
                      <methods>
                      </methods>
                      <lines>
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert
        var classItem = Assert.Single(report.Packages[0].Classes);
        Assert.Equal("", classItem.Name);
    }

    [Fact]
    public void Parse_MethodWithMissingNameAndSignatureAttributes_ReturnsEmptyStrings()
    {
        // Arrange - method without name and signature attributes
        const string xml = """
            <?xml version="1.0"?>
            <coverage line-rate="1.0" branch-rate="1.0" version="1.9" timestamp="0">
              <packages>
                <package name="Test" line-rate="1.0" branch-rate="1.0" complexity="0">
                  <classes>
                    <class name="TestClass" filename="Test.cs" line-rate="1.0" branch-rate="1.0" complexity="0">
                      <methods>
                        <method line-rate="1.0" branch-rate="1.0" complexity="0">
                          <lines>
                            <line number="10" hits="1" branch="false" />
                          </lines>
                        </method>
                      </methods>
                      <lines>
                        <line number="10" hits="1" branch="false" />
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        // Act
        var report = _parser.Parse(xml);

        // Assert
        var method = Assert.Single(report.Packages[0].Classes[0].Methods);
        Assert.Equal("", method.Name);
        Assert.Equal("", method.Signature);
    }
}
