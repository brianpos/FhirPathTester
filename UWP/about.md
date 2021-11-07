
## Introduction ##
This is a simple technical utility to offer a simple UI for testing
fhirpath expressions with R4, STU3 and DSTU2 [HL7 FHIR][fhir-spec] resources.

It can execute the expressions as either an extract (for use in search 
expressions) or validate (for use in StructureDefinition constraints)

New Features in v1.2.0:

* Updated to recent HL7 FHIR NuGet pacakges 3.6.0 (including support for fhirpath aggregte operation)
* Inclusion of beta FHIR R5 version
* Removal of FHIR DSTU2 support
* Custom dateadd function only supports datetime (needs update to core package to support date)

New Features in v1.1.14:

* Drag and drop plain text from outside the application (not just files/web references)

New Features in v1.1.13:

* Now supports HL7 FHIR v4.0.1
* Updated to HL7 FHIR NuGet packages 1.9.0 (last one compatible with DSTU2)
* Now supporting context based processing for testing invariant expressions and Structured Data Capture (SDC) expressions
  * click on the item in the source pane (right side) to set the context once parsed/extracted to set context (shown in bottom right)
* Double clicking an item in the output will highlight where it came from in the source document (experimental)
* 

New Features in v1.0.8:

* Full support for R4
* Ability to convert/pretty print the source between XML/Json (click on the label you want to format the content to)
* Custom fhirpath function LuhnTest()
* Updated to recent HL7 FHIR NuGet packages 1.3.0
* Drag and drop example resource files from the web! (not, does not support authentication to the FHIR Server)
* Hotkeys Ctrl+E and Ctrl+T to the execute and validate buttons
* Supports the Dark system theme
* Display the fhirpath location of the cursor in the status bar once an extract or validate call has been performed

Key features:

* Evaluation [FhirPath][fhirpath-spec] expressions (extract/validate)
* Parsing of the expression against the schema of the example resource type
* Concurrent support for R4, STU3 and DSTU2 (automatically selecting)
* Support for Xml and Json parsers (automatically detecting)
* Drag and drop example resource files from the file-system

Additional custom fhirpath functions:

| Function | Description
| - | - |
| propname | Returns the name of the property at the selected location |
| pathname | Returns the full path of the selected location (includes array selectors at every point) |
| shortpathname | Returns the full path of the selected location (does not include array selectors where the cardinality of the property is 0..1)|
| dateadd(field, amount) | Add a value to a datetime field (field == yy,mm,dd,hh,hi,ss) |
| LuhnTest | Checks the selected value to verify that it conforms to the Luhn Checksum algorithm https://rosettacode.org/wiki/Luhn_test_of_credit_card_numbers#C.23 |
| DoubleMetaphone | Returns the double metaphone approximation of the selected node (will break into multiple words, and return a value for each word) |
| Metaphone | Returns the metaphone approximation of the selected node (will break into multiple words, and return a value for each word) |
| Stem | Returns the Porter 2 Stemming alogorithm for each word in the input location |

## Support 
Issues can be raised in the GitHub repository at [https://github.com/brianpos/fhirpathtester/issues](https://github.com/brianpos/fhirpathtester/issues).
You are welcome to register your bugs and feature suggestions there. 
For questions and broader discussions, use the .NET FHIR Implementers chat on [Zulip][netapi-zulip].

## Contributing ##
I would gladly welcome any contributors!

If you want to participate in this project, I'm using [Git Flow][nvie] for branch management, so please submit your commits using pull requests no on the develop branches mentioned above! 

[fhirpath-spec]: http://hl7.org/fhirpath/
[r4-spec]: http://www.hl7.org/fhir/R4
[stu3-spec]: http://www.hl7.org/fhir/STU3
[dstu2-spec]: http://hl7.org/fhir/DSTU2/index.html
[netapi-zulip]: https://chat.fhir.org/#narrow/stream/dotnet
[netapi-docu]: http://ewoutkramer.github.io/fhir-net-api/docu-index.html
[nvie]: http://nvie.com/posts/a-successful-git-branching-model/


## Licensing
HL7®, FHIR® and the FHIR Mark® are trademarks owned by Health Level Seven International, 
registered with the United States Patent and Trademark Office.
