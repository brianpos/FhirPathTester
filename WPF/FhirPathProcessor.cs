extern alias stu3;
extern alias r4;
extern alias r5;

using Hl7.Fhir.ElementModel;

using f5 = r5.Hl7.Fhir.Model;

using f3 = stu3.Hl7.Fhir.Model;

using f4 = r4.Hl7.Fhir.Model;
using System;
using Hl7.FhirPath.Expressions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.FhirPath;
using Hl7.Fhir.Introspection;
using Hl7.Fhir.Model;

namespace FhirPathTester
{
    public static class FhirPathProcessor
    {
        static public Hl7.FhirPath.FhirPathCompiler _compiler = new Hl7.FhirPath.FhirPathCompiler(CustomFluentPathFunctions.Scope);
        static private readonly ParserSettings _parserSettings = new ParserSettings() { PermissiveParsing = true };
        static internal readonly ModelInspector _mi3 = ModelInspector.ForAssembly(typeof(f3.Patient).Assembly);
        static internal readonly ModelInspector _mi4 = ModelInspector.ForAssembly(typeof(f4.Patient).Assembly);
        static internal readonly ModelInspector _mi5 = ModelInspector.ForAssembly(typeof(f5.Patient).Assembly);

        public static ITypedElement GetResourceNavigatorR5(string text, out string parseErrors)
        {
            var tnSettings = new TypedElementSettings() { ErrorMode = TypedElementSettings.TypeErrorMode.Report };

            Hl7.Fhir.Model.Resource resource = null;
            try
            {
                if (text.StartsWith("{"))
                    resource = new stu3.Hl7.Fhir.Serialization.FhirJsonParser(_parserSettings).Parse<Hl7.Fhir.Model.Resource>(text);
                else
                    resource = new stu3.Hl7.Fhir.Serialization.FhirXmlParser(_parserSettings).Parse<Hl7.Fhir.Model.Resource>(text);
            }
            catch (Exception ex)
            {
                parseErrors = "R5 Resource read error:\r\n" + ex.Message;
                return null;
            }
            parseErrors = null;
            var inputNav = resource.ToTypedElement(_mi5);
            return new ScopedNode(new SqlonfhirScopedNode(inputNav));
        }

        public static ITypedElement GetResourceNavigatorSTU3(string text, out string parseErrors)
        {
            Hl7.Fhir.Model.Resource resource = null;
            try
            {
                if (text.StartsWith("{"))
                    resource = new stu3.Hl7.Fhir.Serialization.FhirJsonParser(_parserSettings).Parse<Hl7.Fhir.Model.Resource>(text);
                else
                    resource = new stu3.Hl7.Fhir.Serialization.FhirXmlParser(_parserSettings).Parse<Hl7.Fhir.Model.Resource>(text);
            }
            catch (Exception ex)
            {
                parseErrors = "STU3 Resource read error:\r\n" + ex.Message;
                return null;
            }
            parseErrors = null;
            var inputNav = resource.ToTypedElement(_mi3);
            return new ScopedNode(new SqlonfhirScopedNode(inputNav));
        }
        public static ITypedElement GetResourceNavigatorR4(string text, out string parseErrors)
        {
            Resource resource = null;
            try
            {
                if (text.StartsWith("{"))
                    resource = new r4.Hl7.Fhir.Serialization.FhirJsonParser(_parserSettings).Parse<Resource>(text);
                else
                    resource = new r4.Hl7.Fhir.Serialization.FhirXmlParser(_parserSettings).Parse<Resource>(text);
            }
            catch (Exception ex)
            {
                parseErrors = "R4 Resource read error:\r\n" + ex.Message;
                return null;
            }
            parseErrors = null;
            var inputNav = resource.ToTypedElement(_mi4);
            return new ScopedNode(new SqlonfhirScopedNode(inputNav));
        }

        public static void ProcessPrepopulatedValues(IEnumerable<ITypedElement> prepopulatedValues, Func<string, string, string> AppendXmlFramentResults, Action<string, bool, string> AppendResults)
        {
            //Hl7.Fhir.Specification.IStructureDefinitionSummaryProvider
            //r4.Hl7.Fhir.Specification.PocoStructureDefinitionSummaryProvider
            if (prepopulatedValues.Count() > 0)
            {
                foreach (var item in prepopulatedValues)
                {
                    string tooltip = item.Location; // Annotation<IShortPathGenerator>()?.ShortPath;
                    if (item.Annotation<IFhirValueProvider>() != null)
                    {
                        foreach (var itemVal in new ITypedElement[] { item }.ToFhirValues().Where(i => i != null))
                        {
                            // output the content as XML fragments
                            string fragment = null;
                            if (item.Definition is PropertyMapping pm)
                            {
                                if (pm.Release == FhirRelease.R4)
                                    fragment = new stu3.Hl7.Fhir.Serialization.FhirXmlSerializer().SerializeToString(itemVal, root: itemVal.TypeName.Replace("#", "."));
                                if (pm.Release == FhirRelease.STU3)
                                    fragment = new r4.Hl7.Fhir.Serialization.FhirXmlSerializer().SerializeToString(itemVal, root: itemVal.TypeName.Replace("#", "."));
                                if (pm.Release == FhirRelease.R5)
                                    fragment = new r5.Hl7.Fhir.Serialization.FhirXmlSerializer().SerializeToString(itemVal, root: itemVal.TypeName.Replace("#", "."));
                            }
                            fragment = AppendXmlFramentResults(fragment, tooltip);
                        }
                    }
                    else if (item is ITypedElement te)
                    {
                        AppendResults($"<{te.InstanceType} value=\"{te.Value}\">", false, tooltip);
                    }
                }
            }
        }

        public static void CheckExpression(ITypedElement inputNav, Hl7.FhirPath.Expressions.Expression expr, Action<string, bool, string> AppendResults, Action ResetResults)
        {
            ExpressionElementContext context = new ExpressionElementContext(inputNav.Name);
            if (inputNav is IFhirValueProvider pn)
            {
                if (pn.FhirValue is f5.Questionnaire q5)
                {
                    context._q2 = q5;
                }
                if (pn.FhirValue is f3.Questionnaire q3)
                {
                    context._q3 = q3;
                }
                if (pn.FhirValue is f4.Questionnaire q4)
                {
                    context._q4 = q4;
                }
            }
            ResetResults();
            CheckExpression(expr, "", context, AppendResults);
        }

        private static ExpressionElementContext CheckExpression(Hl7.FhirPath.Expressions.Expression expr, string prefix, ExpressionElementContext context, Action<string, bool, string> AppendResults)
        {
            if (expr is ChildExpression)
            {
                var func = expr as ChildExpression;
                var focusContext = CheckExpression(func.Focus, prefix + "-- ", context, AppendResults);
                var childContext = focusContext.Child(func.ChildName);
                if (childContext != null)
                {
                    if (focusContext._q4 != null)
                    {
                        if (func.ChildName == "item")
                        {
                            childContext._4is = new List<f4.Questionnaire.ItemComponent>();
                            childContext._4is.AddRange(focusContext._q4.Item);
                        }
                    }
                    if (focusContext._q3 != null)
                    {
                        if (func.ChildName == "item")
                        {
                            childContext._3is = new List<f3.Questionnaire.ItemComponent>();
                            childContext._3is.AddRange(focusContext._q3.Item);
                        }
                    }
                    if (focusContext._q2 != null)
                    {
                        if (func.ChildName == "group")
                        {
                            childContext._2gs = new List<f5.Questionnaire.ItemComponent>();
                            childContext._2gs.AddRange(focusContext._q2.Item);
                        }
                    }

                    if (focusContext._4is != null)
                    {
                        if (func.ChildName == "item")
                        {
                            childContext._4is = new List<f4.Questionnaire.ItemComponent>();
                            foreach (var item in focusContext._4is)
                            {
                                if (item.Item != null)
                                    childContext._4is.AddRange(item.Item);
                            }
                        }
                    }
                    if (focusContext._3is != null)
                    {
                        if (func.ChildName == "item")
                        {
                            childContext._3is = new List<f3.Questionnaire.ItemComponent>();
                            foreach (var item in focusContext._3is)
                            {
                                if (item.Item != null)
                                    childContext._3is.AddRange(item.Item);
                            }
                        }
                    }
                    if (focusContext._2gs != null)
                    {
                        if (func.ChildName == "item")
                        {
                            childContext._2gs = new List<f5.Questionnaire.ItemComponent>();
                            foreach (var item in focusContext._2gs)
                            {
                                if (item.Item != null)
                                    childContext._2gs.AddRange(item.Item);
                            }
                        }
                    }
                    AppendResults($"{prefix}{func.ChildName}", false, childContext.Tooltip());
                    return childContext;
                }
                else
                {
                    AppendResults($"{prefix}{func.ChildName} *invalid property name*", true, null);
                }
                return context;
            }
            if (expr is FunctionCallExpression)
            {
                var func = expr as FunctionCallExpression;
                var funcs = _compiler.Symbols.Filter(func.FunctionName, func.Arguments.Count() + 1);
                if (funcs.Count() == 0 && !(expr is BinaryExpression))
                {
                    AppendResults($"{prefix}{func.FunctionName} *invalid function name*", true, null);
                }
                else
                {
                    AppendResults($"{prefix}{func.FunctionName}", false, null);
                }
                var focusContext = CheckExpression(func.Focus, prefix + "-- ", context, AppendResults);

                if (func.FunctionName == "binary.as")
                {
                    if (func.Arguments.Count() != 2)
                    {
                        AppendResults($"{prefix}{func.FunctionName} INVALID AS Operation", true, null);
                        return focusContext;
                    }
                    var argContextResult = CheckExpression(func.Arguments.First(), prefix + "    ", focusContext, AppendResults);
                    var typeArg = func.Arguments.Skip(1).FirstOrDefault() as ConstantExpression;
                    string typeCast = typeArg?.Value as string;
                    argContextResult.RestrictToType(typeCast);
                    return argContextResult;
                }
                else if (func.FunctionName == "resolve")
                {
                    // need to check what the available outcomes of resolving this are, and switch types to this
                }
                else
                {
                    // if this is a where operation and the context inside is a linkId = , then check that the linkId is in context
                    if (func.FunctionName == "where" && func.Arguments.Count() == 1 && (func.Arguments.First() as BinaryExpression)?.Op == "=")
                    {
                        var op = func.Arguments.First() as BinaryExpression;
                        var argContextResult = CheckExpression(op, prefix + "    ", focusContext, AppendResults);

                        // Filter the values that are not in this set
                        focusContext._4is = argContextResult._4is;
                        focusContext._3is = argContextResult._3is;
                        focusContext._2gs = argContextResult._2gs;
                        focusContext._2qs = argContextResult._2qs;
                    }
                    else
                    {
                        foreach (var item in func.Arguments)
                        {
                            var argContextResult = CheckExpression(item, prefix + "    ", focusContext, AppendResults);
                        }
                    }
                    if (func.FunctionName == "binary.=")
                    {
                        ChildExpression prop = (ChildExpression)func.Arguments.Where(a => a is ChildExpression).FirstOrDefault();
                        ConstantExpression value = (ConstantExpression)func.Arguments.Where(a => a is ConstantExpression).FirstOrDefault();
                        if (prop?.ChildName == "linkId" && value != null)
                        {
                            var groupLinkIds = focusContext._2gs?.Select(i => i.LinkId).ToArray();
                            var questionLinkIds = focusContext._2qs?.Select(i => i.LinkId).ToArray();
                            var item4Ids = focusContext._4is?.Select(i => i.LinkId).ToArray();
                            var item3Ids = focusContext._3is?.Select(i => i.LinkId).ToArray();

                            // filter out all of the other linkIds from the list
                            focusContext._4is?.RemoveAll(i => i.LinkId != value.Value as string);
                            focusContext._3is?.RemoveAll(i => i.LinkId != value.Value as string);
                            focusContext._2gs?.RemoveAll(g => g.LinkId != value.Value as string);
                            focusContext._2qs?.RemoveAll(q => q.LinkId != value.Value as string);

                            // Validate that there is an item with this value that is reachable
                            if (focusContext._2gs?.Count() == 0 || focusContext._2qs?.Count() == 0)
                            {
                                // this linkId didn't exist in this context!
                                string toolTip = "Available LinkIds:";
                                if (groupLinkIds != null)
                                    toolTip += $"\r\nGroup: {String.Join(", ", groupLinkIds)}";
                                if (questionLinkIds != null)
                                    toolTip += $"\r\nQuestion: {String.Join(", ", questionLinkIds)}";
                                AppendResults($"{prefix}{func.FunctionName} LinkId is not valid in this context", true, toolTip);
                            }
                            if (focusContext._3is?.Count() == 0)
                            {
                                // this linkId didn't exist in this context!
                                string toolTip = "Available LinkIds:";
                                if (item3Ids != null)
                                    toolTip += $"\r\nItems: {String.Join(", ", item3Ids)}";
                                AppendResults($"{prefix}{func.FunctionName} LinkId is not valid in this context", true, toolTip);
                            }
                            if (focusContext._4is?.Count() == 0)
                            {
                                // this linkId didn't exist in this context!
                                string toolTip = "Available LinkIds:";
                                if (item4Ids != null)
                                    toolTip += $"\r\nItems: {String.Join(", ", item4Ids)}";
                                AppendResults($"{prefix}{func.FunctionName} LinkId is not valid in this context", true, toolTip);
                            }
                        }
                    }
                }

                return focusContext;
            }
            //else if (expr is BinaryExpression)
            //{
            //    var func = expr as BinaryExpression;
            //    sb.AppendLine(func.FunctionName);
            //    CheckExpression(func.Left, sb);
            //    sb.AppendLine(func.Op);
            //    CheckExpression(func.Right, sb);
            //    return;
            //}
            else if (expr is ConstantExpression)
            {
                var func = expr as ConstantExpression;
                AppendResults($"{prefix}{func.Value.ToString()} (constant)", false, null);
                return null; // context doesn't propogate from this
            }
            else if (expr is VariableRefExpression)
            {
                var func = expr as VariableRefExpression;
                // sb.AppendFormat("{0}{1} (variable ref)\r\n", prefix, func.Name);
                return context;
            }
            AppendResults(expr.GetType().ToString(), false, null);
            return context;
        }

        public static void PretifyXML(string text, Action<string> setText)
        {
            // prettify the XML (and convert to XML if it wasn't already)
            Resource resource4 = null;
            string contentAsXML;
            try
            {
                if (text.StartsWith("{"))
                {
                    resource4 = new r4.Hl7.Fhir.Serialization.FhirJsonParser().Parse<Resource>(text);
                    contentAsXML = new r4.Hl7.Fhir.Serialization.FhirXmlSerializer().SerializeToString(resource4);
                }
                else
                    contentAsXML = text;
                var doc = System.Xml.Linq.XDocument.Parse(contentAsXML);
                setText(doc.ToString(System.Xml.Linq.SaveOptions.None));
            }
            catch (Exception ex4)
            {
                Resource resource3 = null;
                try
                {
                    if (text.StartsWith("{"))
                    {
                        resource3 = new stu3.Hl7.Fhir.Serialization.FhirJsonParser().Parse<Resource>(text);
                        contentAsXML = new stu3.Hl7.Fhir.Serialization.FhirXmlSerializer().SerializeToString(resource3);
                    }
                    else
                        contentAsXML = text;
                    var doc = System.Xml.Linq.XDocument.Parse(contentAsXML);
                    setText(doc.ToString(System.Xml.Linq.SaveOptions.None));
                }
                catch (Exception ex3)
                {
                    Resource resource2 = null;
                    try
                    {
                        if (text.StartsWith("{"))
                        {
                            resource2 = new r5::Hl7.Fhir.Serialization.FhirJsonParser().Parse<Resource>(text);
                            contentAsXML = new r5::Hl7.Fhir.Serialization.FhirXmlSerializer().SerializeToString(resource2);
                        }
                        else
                            contentAsXML = text;
                        var doc = System.Xml.Linq.XDocument.Parse(contentAsXML);
                        setText(doc.ToString(System.Xml.Linq.SaveOptions.None));
                    }
                    catch (Exception ex2)
                    {
                        System.Diagnostics.Debug.WriteLine(ex4.Message);
                        System.Diagnostics.Debug.WriteLine(ex3.Message);
                        System.Diagnostics.Debug.WriteLine(ex2.Message);
                    }
                }
            }
        }

        public static void PretifyJson(string text, Action<string> setText)
        {
            // prettify the Json (and convert to Json if it wasn't already)
            Resource resource4 = null;
            string contentAsJson;
            try
            {
                if (text.StartsWith("{"))
                {
                    contentAsJson = text;
                }
                else
                {
                    resource4 = new r4.Hl7.Fhir.Serialization.FhirXmlParser(_parserSettings).Parse<Resource>(text);
                    contentAsJson = new r4.Hl7.Fhir.Serialization.FhirJsonSerializer().SerializeToString(resource4);
                }
                var sr = new System.IO.StringReader(contentAsJson);
                var reader = new JsonTextReader(sr);
                var doc = JObject.Load(reader);
                setText(doc.ToString(Formatting.Indented));
            }
            catch (Exception ex4)
            {
                Resource resource3 = null;
                try
                {
                    if (text.StartsWith("{"))
                    {
                        contentAsJson = text;
                    }
                    else
                    {
                        resource3 = new stu3.Hl7.Fhir.Serialization.FhirXmlParser(_parserSettings).Parse<Resource>(text);
                        contentAsJson = new r5::Hl7.Fhir.Serialization.FhirJsonSerializer().SerializeToString(resource3);
                    }
                    var sr = new System.IO.StringReader(contentAsJson);
                    var reader = new JsonTextReader(sr);
                    var doc = JObject.Load(reader);
                    setText(doc.ToString(Formatting.Indented));
                }
                catch (Exception ex3)
                {
                    Resource resource2 = null;
                    try
                    {
                        if (text.StartsWith("{"))
                        {
                            contentAsJson = text;
                        }
                        else
                        {
                            resource2 = new r5::Hl7.Fhir.Serialization.FhirXmlParser(_parserSettings).Parse<Resource>(text);
                            contentAsJson = new r5::Hl7.Fhir.Serialization.FhirJsonSerializer().SerializeToString(resource2);
                        }
                        var sr = new System.IO.StringReader(text);
                        var reader = new JsonTextReader(sr);
                        var doc = JObject.Load(reader);
                        setText(doc.ToString(Formatting.Indented));
                    }
                    catch (Exception ex2)
                    {
                        System.Diagnostics.Debug.WriteLine(ex4.Message);
                        System.Diagnostics.Debug.WriteLine(ex3.Message);
                        System.Diagnostics.Debug.WriteLine(ex2.Message);
                    }
                }
            }
        }
    }

    public class ExpressionElementContext
    {
        public ExpressionElementContext(string typeName)
        {
            _typeName = typeName;
            if (!string.IsNullOrEmpty(typeName))
            {
                var t4 = r4.Hl7.Fhir.Model.ModelInfo.GetTypeForFhirType(typeName);
                if (t4 != null)
                {
                    try
                    {
                        _cm4 = new List<ClassMapping>() { FhirPathProcessor._mi4.FindOrImportClassMapping(t4) };
                    }
                    catch
                    {
                    }
                }
                var t3 = stu3.Hl7.Fhir.Model.ModelInfo.GetTypeForFhirType(typeName);
                if (t3 != null)
                {
                    try
                    {
                        _cm3 = new List<ClassMapping>() { FhirPathProcessor._mi3.FindOrImportClassMapping(t3) };
                    }
                    catch
                    {
                    }
                }
                var t2 = r5::Hl7.Fhir.Model.ModelInfo.GetTypeForFhirType(typeName);
                if (t2 != null)
                {
                    try
                    {
                        _cm2 = new List<ClassMapping>() { FhirPathProcessor._mi5.FindOrImportClassMapping(t2) };
                    }
                    catch
                    {
                    }
                }
            }
        }

        private ExpressionElementContext()
        {
            _cm4 = new List<ClassMapping>();
            _cm3 = new List<ClassMapping>();
            _cm2 = new List<ClassMapping>();
        }

        // Questionnaire Context information when processing validation against a questionnaire
        internal f5.Questionnaire _q2;
        internal f3.Questionnaire _q3;
        internal f4.Questionnaire _q4;
        internal List<f5.Questionnaire.ItemComponent> _2gs;
        internal List<f5.Questionnaire.ItemComponent> _2qs;
        internal List<f3.Questionnaire.ItemComponent> _3is;
        internal List<f4.Questionnaire.ItemComponent> _4is;

        // Where this is the context of a class
        string _typeName;
        List<ClassMapping> _cm4;
        List<ClassMapping> _cm3;
        List<ClassMapping> _cm2;

        public string possibleTypes()
        {
            return string.Join(", ", _cm2.Select(t => "::" + t.Name).Union(_cm3.Select(t => "::" + t.Name)).Union(_cm4.Select(t => "::" + t.Name)));
        }

        bool IsMatchingPropName(string propertyName, PropertyMapping pm)
        {
            if (propertyName == pm.Name)
                return true;
            if (propertyName + "[x]" == pm.Name)
                return true;
            //if (!pm.MatchesSuffixedName(propertyName))
            //    return false;
            string ExpectedType = propertyName.Substring(pm.Name.Length);
            if (Enum.TryParse<f4.FHIRAllTypes>(ExpectedType, out var result4))
            {
                if (String.Compare(ExpectedType, Hl7.Fhir.Utility.EnumUtility.GetLiteral(result4), true) != 0)
                    return false;
                if (f4.ModelInfo.IsDataType(result4) || f4.ModelInfo.IsPrimitive(result4))
                    return true;
            }
            if (Enum.TryParse<f3.FHIRAllTypes>(ExpectedType, out var result3))
            {
                if (String.Compare(ExpectedType, Hl7.Fhir.Utility.EnumUtility.GetLiteral(result3), true) != 0)
                    return false;
                if (f3.ModelInfo.IsDataType(result3) || f3.ModelInfo.IsPrimitive(result3))
                    return true;
            }
            if (Enum.TryParse<f5.FHIRAllTypes>(ExpectedType, out var result))
            {
                if (String.Compare(ExpectedType, Hl7.Fhir.Utility.EnumUtility.GetLiteral(result), true) != 0)
                    return false;
                if (f5.ModelInfo.IsDataType(ExpectedType) || f5.ModelInfo.IsPrimitive(ExpectedType))
                    return true;
            }
            return false;
        }


        bool HasProperty(string propName,
                out List<PropertyMapping> dstu2,
                out List<PropertyMapping> stu3,
                out List<PropertyMapping> r4)
        {
            if (_cm4 == null)
                r4 = null;
            else
                r4 = _cm4.Select(t => t.FindMappedElementByName(propName)).Where(t => t != null && IsMatchingPropName(propName, t)).ToList();
            if (_cm3 == null)
                stu3 = null;
            else
                stu3 = _cm3.Select(t => t.FindMappedElementByName(propName)).Where(t => t != null && IsMatchingPropName(propName, t)).ToList();
            if (_cm2 == null)
                dstu2 = null;
            else
                dstu2 = _cm2.Select(t => t.FindMappedElementByName(propName)).Where(t => t != null && IsMatchingPropName(propName, t)).ToList();
            return (dstu2?.Count() > 0) || (stu3?.Count() > 0) || (r4?.Count() > 0);
        }

        public ExpressionElementContext Child(string propertyName)
        {
            // Special case for the top level node
            if (propertyName == _typeName)
                return this;

            if (!HasProperty(propertyName, out var dstu2, out var stu3, out var r4))
                return null;

            var newContext = new ExpressionElementContext();
            /*
            if (r4?.Any() == true)
            {
                foreach (var item in r4)
                {
                    try
                    {
                        if (item.Choice == ChoiceType.DatatypeChoice)
                        {
                            if (item.Name != propertyName && IsMatchingPropName(item.MatchesSuffixedName(propertyName))
                            {
                                string ExpectedType = propertyName.Substring(item.Name.Length);
                                if (Enum.TryParse<f4.FHIRAllTypes>(ExpectedType, out var result))
                                {
                                    if (f4.ModelInfo.IsDataType(result) || f4.ModelInfo.IsPrimitive(result))
                                    {
                                        // may need to recheck that a typename of value23 won't work here
                                        string name = f4.ModelInfo.FhirTypeToFhirTypeName(result);
                                        var t = f4.ModelInfo.GetTypeForFhirType(name);
                                        newContext._cm4.Add(ClassMapping.Create(t));
                                    }
                                }
                            }
                            else
                            {
                                // would be great to be able to filter through only the viable types
                                // but we don't have that, so just enumerate the available datatypes
                                foreach (f4.FHIRAllTypes ev in Enum.GetValues(typeof(f4.FHIRAllTypes)))
                                {
                                    if (f4.ModelInfo.IsDataType(ev) && !f4.ModelInfo.IsCoreSuperType(ev))
                                    {
                                        string name = f4.ModelInfo.FhirTypeToFhirTypeName(ev);
                                        var t = f4.ModelInfo.GetTypeForFhirType(name);
                                        newContext._cm4.Add(ClassMapping.Create(t));
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (item.ElementType != typeof(string)) // (only occurs for extension.url and elementdefinition.id)
                                newContext._cm4.Add(ClassMapping.Create(item.ElementType));
                        }
                    }
                    catch
                    {
                    }
                }
            }
            if (stu3?.Any() == true)
            {
                foreach (var item in stu3)
                {
                    try
                    {
                        if (item.Choice == ChoiceType.DatatypeChoice)
                        {
                            if (item.Name != propertyName && item.MatchesSuffixedName(propertyName))
                            {
                                string ExpectedType = propertyName.Substring(item.Name.Length);
                                if (Enum.TryParse<f3.FHIRAllTypes>(ExpectedType, out var result))
                                {
                                    if (f3.ModelInfo.IsDataType(result) || f3.ModelInfo.IsPrimitive(result))
                                    {
                                        // may need to recheck that a typename of value23 won't work here
                                        string name = f3.ModelInfo.FhirTypeToFhirTypeName(result);
                                        var t = f3.ModelInfo.GetTypeForFhirType(name);
                                        newContext._cm3.Add(ClassMapping.Create(t));
                                    }
                                }
                            }
                            else
                            {
                                // would be great to be able to filter through only the viable types
                                // but we don't have that, so just enumerate the available datatypes
                                foreach (f3.FHIRAllTypes ev in Enum.GetValues(typeof(f3.FHIRAllTypes)))
                                {
                                    if (f3.ModelInfo.IsDataType(ev) && !f3.ModelInfo.IsCoreSuperType(ev))
                                    {
                                        string name = f3.ModelInfo.FhirTypeToFhirTypeName(ev);
                                        var t = f3.ModelInfo.GetTypeForFhirType(name);
                                        newContext._cm3.Add(ClassMapping.Create(t));
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (item.ImplementingType != typeof(string)) // (only occurs for extension.url and elementdefinition.id)
                                newContext._cm3.Add(ClassMapping.Create(item.ImplementingType));
                        }
                    }
                    catch
                    {
                    }
                }
            }

            if (dstu2?.Any() == true)
            {
                foreach (var item in dstu2)
                {
                    try
                    {
                        if (item.Choice == ChoiceType.DatatypeChoice)
                        {
                            if (item.Name != propertyName && item.MatchesSuffixedName(propertyName))
                            {
                                string ExpectedType = propertyName.Substring(item.Name.Length);
                                if (Enum.TryParse<f5.FHIRDefinedType>(ExpectedType, out var result))
                                {
                                    if (f5.ModelInfo.IsDataType(result) || f5.ModelInfo.IsPrimitive(result))
                                    {
                                        // may need to recheck that a typename of value23 won't work here
                                        string name = f5.ModelInfo.FhirTypeToFhirTypeName(result);
                                        var t = f5.ModelInfo.GetTypeForFhirType(name);
                                        newContext._cm2.Add(ClassMapping.Create(t));
                                    }
                                }
                            }
                            else
                            {
                                // would be great to be able to filter through only the viable types
                                // but we don't have that, so just enumerate the available datatypes
                                foreach (f5.FHIRDefinedType ev in Enum.GetValues(typeof(f5.FHIRDefinedType)))
                                {
                                    if (f5.ModelInfo.IsDataType(ev) && !f5.ModelInfo.IsCoreSuperType(ev))
                                    {
                                        string name = f5.ModelInfo.FhirTypeToFhirTypeName(ev);
                                        var t = f5.ModelInfo.GetTypeForFhirType(name);
                                        newContext._cm2.Add(ClassMapping.Create(t));
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (item.ElementType != typeof(string)) // (only occurs for extension.url and elementdefinition.id)
                                newContext._cm2.Add(ClassMapping.Create(item.ElementType));
                        }
                    }
                    catch
                    {
                    }
                }
            }
            */
            return newContext;
        }

        internal void RestrictToType(string typeCast)
        {
            if (Enum.TryParse<f4.FHIRAllTypes>(typeCast, out var result4))
            {
                if (f4.ModelInfo.IsDataType(result4) || f4.ModelInfo.IsPrimitive(result4))
                {
                    _cm4 = _cm4.Where(t => t.Name == typeCast).ToList();
                }
            }
            if (Enum.TryParse<f3.FHIRAllTypes>(typeCast, out var result))
            {
                if (f3.ModelInfo.IsDataType(result) || f3.ModelInfo.IsPrimitive(result))
                {
                    _cm3 = _cm3.Where(t => t.Name == typeCast).ToList();
                }
            }
            if (Enum.TryParse<f5.FHIRAllTypes>(typeCast, out var result2))
            {
                if (f5.ModelInfo.IsDataType(typeCast) || f5.ModelInfo.IsPrimitive(typeCast))
                {
                    _cm2 = _cm2.Where(t => t.Name == typeCast).ToList();
                }
            }
        }

        internal string Tooltip()
        {
            StringBuilder sb = new StringBuilder();
            if (_cm4?.Any() == true)
            {
                sb.Append("R4:");
                foreach (var i4 in _cm4.Select(c => c.Name).Distinct())
                {
                    sb.Append($" {i4}");
                }
            }
            if (_cm3?.Any() == true)
            {
                if (_cm4?.Any() == true)
                    sb.AppendLine();
                sb.Append("STU3:");
                foreach (var i3 in _cm3.Select(c => c.Name).Distinct())
                {
                    sb.Append($" {i3}");
                }
            }
            if (_cm2?.Any() == true)
            {
                if (_cm3?.Any() == true || _cm4?.Any() == true)
                    sb.AppendLine();
                sb.Append("DSTU2:");
                foreach (var i2 in _cm2.Select(c => c.Name).Distinct())
                {
                    sb.Append($" {i2}");
                }
            }
            var groupLinkIds = _2gs?.Select(i => i.LinkId).ToArray();
            var questionLinkIds = _2qs?.Select(i => i.LinkId).ToArray();
            if (groupLinkIds != null)
                sb.Append($"\r\nGroup LinkIds: {String.Join(", ", groupLinkIds)}");
            if (questionLinkIds != null)
                sb.Append($"\r\nQuestion LinkIds: {String.Join(", ", questionLinkIds)}");

            var itemLinkIds3 = _3is?.Select(i => i.LinkId).ToArray();
            if (itemLinkIds3 != null)
                sb.Append($"\r\nItem3 LinkIds: {String.Join(", ", itemLinkIds3)}");

            var itemLinkIds4 = _4is?.Select(i => i.LinkId).ToArray();
            if (itemLinkIds4 != null)
                sb.Append($"\r\nItem4 LinkIds: {String.Join(", ", itemLinkIds4)}");

            return sb.ToString();
        }
    }

    public class HistoryItemDetails
    {
        public HistoryItemDetails()
        {
        }
        public HistoryItemDetails(string resourceContent, string expression)
        {
            Expression = expression;
            ResourceContent = resourceContent;
            Stored = DateTime.Now;
            Header = String.Join("\n", resourceContent.Split(new[] { '\n', '\r' }).Take(4));
        }
        public string Expression { get; set; }
        public string ResourceContent { get; set; }
        public DateTime Stored { get; set; }
        public string Header { get; set; }
    }
}
