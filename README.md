
## Introduction ##
This is a simple technical utility to offer a simple UI for testing
fhirpath expressions with DSTU2 and STU3 [HL7 FHIR][fhir-spec] resources.

It can execute the expressions as either an extract (for use in search 
expressions) or validate (for use in StructureDefinition constraints)

Key features:
* Evaluation [FhirPath][fhirpath-spec] expressions (extract/validate)
* Parsing of the expression against the schema of the example resource type
* Concurrent support for STU3 and DSTU2 (automatically selecting)
* Support for Xml and Json parsers (automatically detecting)
* Drag and drop example resource files from the file-system

Technically the utility is:
* built on the Microsoft .NET (dotnet) platform
* WPF and UWP versions available
  * Code currently duplicated between the 2 
  (I plan to share the core processing file sometime)
* uses the HL7 FHIR reference assemblies
  * *Core* (NuGet packages starting with `Hl7.Fhir.<version>`) - contains the FhirClient and parsers (Both DSTU2 and STU3)
  * *FhirPath* (NuGet package `Hl7.FhirPath`) - the FhirPath evaluator, used by the Core and Specification assemblies
  * *Support* (NuGet package `Hl7.Fhir.Support`) - a library with interfaces, abstractions and utility methods that are used by the other packages

> **Note**: Both the UWP and WPF projects include NuGet references to both
> the DSTU2 and STU3 packages at the same time.
> This does create a clash of namespaces, which I use the extern alias feature
> in .net to resolve.
 
The project file needs to be manually edited to support the setting of 
the assembly reference alias, as shown below.

```
  <Target Name="ChangeAliasesOfStrongNameAssemblies" BeforeTargets="FindReferenceAssembliesForReferences;ResolveReferences">
    <ItemGroup>
      <ReferencePath Condition="'%(FileName)' == 'Hl7.Fhir.DSTU2.Core'">
        <Aliases>dstu2</Aliases>
      </ReferencePath>
    </ItemGroup>
    <ItemGroup>
      <ReferencePath Condition="'%(FileName)' == 'Hl7.Fhir.STU3.Core'">
        <Aliases>stu3</Aliases>
      </ReferencePath>
    </ItemGroup>
  </Target>
```
*The name of the target is not important, it can be anything.*


## Support 
Issues can be raised in the GitHub repository at [https://github.com/brianpos/fhirpathtester/issues](https://github.com/brianpos/fhirpathtester/issues).
You are welcome to register your bugs and feature suggestions there. 
For questions and broader discussions, use the .NET FHIR Implementers chat on [Zulip][netapi-zulip].

## Contributing ##
I would gladly welcome any contributors!

If you want to participate in this project, I'm using [Git Flow][nvie] for branch management, so please submit your commits using pull requests no on the develop branches mentioned above! 

[fhirpath-spec]: http://hl7.org/fhirpath/
[stu3-spec]: http://www.hl7.org/fhir
[dstu2-spec]: http://hl7.org/fhir/DSTU2/index.html
[netapi-zulip]: https://chat.fhir.org/#narrow/stream/dotnet
[netapi-docu]: http://ewoutkramer.github.io/fhir-net-api/docu-index.html
[nvie]: http://nvie.com/posts/a-successful-git-branching-model/

### GIT branching strategy
- [NVIE](http://nvie.com/posts/a-successful-git-branching-model/)
- Or see: [Git workflow](https://www.atlassian.com/git/workflows#!workflow-gitflow)

## Licensing
HL7®, FHIR® and the FHIR Mark® are trademarks owned by Health Level Seven International, 
registered with the United States Patent and Trademark Office.
