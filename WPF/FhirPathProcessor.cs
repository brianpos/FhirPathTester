extern alias dstu2;
extern alias stu3;
extern alias r4;

using Hl7.Fhir.ElementModel;

using fp2 = dstu2.Hl7.Fhir.FhirPath;
using f2 = dstu2.Hl7.Fhir.Model;

using fp3 = stu3.Hl7.Fhir.FhirPath;
using f3 = stu3.Hl7.Fhir.Model;

using fp4 = r4.Hl7.Fhir.FhirPath;
using f4 = r4.Hl7.Fhir.Model;
using System;
using Hl7.FhirPath.Expressions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace FhirPathTester
{
    public static class FhirPathProcessor
    {
        static public Hl7.FhirPath.FhirPathCompiler _compiler = new Hl7.FhirPath.FhirPathCompiler(CustomFluentPathFunctions.Scope);

        public static ITypedElement GetResourceNavigatorDSTU2(string text, out string parseErrors)
        {
            f2.Resource resource = null;
            try
            {
                if (text.StartsWith("{"))
                    resource = new dstu2.Hl7.Fhir.Serialization.FhirJsonParser(new dstu2.Hl7.Fhir.Serialization.ParserSettings() { PermissiveParsing = true }).Parse<f2.Resource>(text);
                else
                    resource = new dstu2.Hl7.Fhir.Serialization.FhirXmlParser(new dstu2.Hl7.Fhir.Serialization.ParserSettings() { PermissiveParsing = true }).Parse<f2.Resource>(text);
            }
            catch (Exception ex)
            {
                parseErrors = "DSTU2 Resource read error:\r\n" + ex.Message;
                return null;
            }
            parseErrors = null;
            var inputNav = dstu2.Hl7.Fhir.ElementModel.PocoNavigatorExtensions.ToTypedElement(resource);
            return inputNav;
        }
        public static ITypedElement GetResourceNavigatorSTU3(string text, out string parseErrors)
        {
            f3.Resource resource = null;
            try
            {
                if (text.StartsWith("{"))
                    resource = new stu3.Hl7.Fhir.Serialization.FhirJsonParser(new stu3.Hl7.Fhir.Serialization.ParserSettings() { PermissiveParsing = true }).Parse<f3.Resource>(text);
                else
                    resource = new stu3.Hl7.Fhir.Serialization.FhirXmlParser(new stu3.Hl7.Fhir.Serialization.ParserSettings() { PermissiveParsing = true }).Parse<f3.Resource>(text);
            }
            catch (Exception ex)
            {
                parseErrors = "STU3 Resource read error:\r\n" + ex.Message;
                return null;
            }
            parseErrors = null;
            var inputNav = stu3.Hl7.Fhir.ElementModel.PocoNavigatorExtensions.ToTypedElement(resource);
            return inputNav;
        }
        public static ITypedElement GetResourceNavigatorR4(string text, out string parseErrors)
        {
            f4.Resource resource = null;
            try
            {
                if (text.StartsWith("{"))
                    resource = new r4.Hl7.Fhir.Serialization.FhirJsonParser(new r4.Hl7.Fhir.Serialization.ParserSettings() { }).Parse<f4.Resource>(text);
                else
                    resource = new r4.Hl7.Fhir.Serialization.FhirXmlParser(new r4.Hl7.Fhir.Serialization.ParserSettings() { }).Parse<f4.Resource>(text);
            }
            catch (Exception ex)
            {
                parseErrors = "R4 Resource read error:\r\n" + ex.Message;
                return null;
            }
            parseErrors = null;
            var inputNav = r4.Hl7.Fhir.ElementModel.PocoNavigatorExtensions.ToTypedElement(resource);
            return inputNav;
        }

        public static void CheckExpression(ITypedElement inputNav, Hl7.FhirPath.Expressions.Expression expr, Action<string, bool, string> AppendResults, Action ResetResults)
        {
            ExpressionElementContext context = new ExpressionElementContext(inputNav.Name);
            if (inputNav is dstu2::Hl7.Fhir.ElementModel.IFhirValueProvider pn2)
            {
                if (pn2.FhirValue is f2.Questionnaire q)
                {
                    context._q2 = q;
                }
            }
            else if (inputNav is stu3::Hl7.Fhir.ElementModel.IFhirValueProvider pn3)
            {
                if (pn3.FhirValue is f3.Questionnaire q)
                {
                    context._q3 = q;
                }
            }
            else if (inputNav is r4::Hl7.Fhir.ElementModel.IFhirValueProvider pn4)
            {
                if (pn4.FhirValue is f4.Questionnaire q)
                {
                    context._q4 = q;
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
                    if (focusContext._q2 != null)
                    {
                        if (func.ChildName == "group")
                        {
                            childContext._2gs = new List<f2.Questionnaire.GroupComponent>();
                            childContext._2gs.Add(focusContext._q2.Group);
                        }
                    }
                    if (focusContext._2gs != null)
                    {
                        if (func.ChildName == "group")
                        {
                            childContext._2gs = new List<f2.Questionnaire.GroupComponent>();
                            foreach (var item in focusContext._2gs)
                            {
                                if (item.Group != null)
                                    childContext._2gs.AddRange(item.Group);
                            }
                        }
                        else if (func.ChildName == "question")
                        {
                            childContext._2qs = new List<f2.Questionnaire.QuestionComponent>();
                            foreach (var item in focusContext._2gs)
                            {
                                if (item.Question != null)
                                    childContext._2qs.AddRange(item.Question);
                            }
                        }
                    }
                    if (focusContext._2qs != null)
                    {
                        if (func.ChildName == "group")
                        {
                            childContext._2gs = new List<f2.Questionnaire.GroupComponent>();
                            foreach (var item in focusContext._2qs)
                            {
                                if (item.Group != null)
                                    childContext._2gs.AddRange(item.Group);
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

                            // filter out all of the other linkIds from the list
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
            f3.Resource resource = null;
            string contentAsXML;
            try
            {
                if (text.StartsWith("{"))
                {
                    resource = new stu3.Hl7.Fhir.Serialization.FhirJsonParser().Parse<f3.Resource>(text);
                    contentAsXML = new stu3.Hl7.Fhir.Serialization.FhirXmlSerializer().SerializeToString(resource);
                }
                else
                    contentAsXML = text;
                var doc = System.Xml.Linq.XDocument.Parse(contentAsXML);
                setText(doc.ToString(System.Xml.Linq.SaveOptions.None));
            }
            catch (Exception ex3)
            {
                f2.Resource resource2 = null;
                try
                {
                    if (text.StartsWith("{"))
                    {
                        resource2 = new dstu2.Hl7.Fhir.Serialization.FhirJsonParser().Parse<f2.Resource>(text);
                        contentAsXML = new dstu2.Hl7.Fhir.Serialization.FhirXmlSerializer().SerializeToString(resource2);
                    }
                    else
                        contentAsXML = text;
                    var doc = System.Xml.Linq.XDocument.Parse(contentAsXML);
                    setText(doc.ToString(System.Xml.Linq.SaveOptions.None));
                }
                catch (Exception ex2)
                {
                    f4.Resource resource4 = null;
                    try
                    {
                        if (text.StartsWith("{"))
                        {
                            resource4 = new r4.Hl7.Fhir.Serialization.FhirJsonParser().Parse<f4.Resource>(text);
                            contentAsXML = new r4.Hl7.Fhir.Serialization.FhirXmlSerializer().SerializeToString(resource4);
                        }
                        else
                            contentAsXML = text;
                        var doc = System.Xml.Linq.XDocument.Parse(contentAsXML);
                        setText(doc.ToString(System.Xml.Linq.SaveOptions.None));
                    }
                    catch (Exception ex4)
                    {
                    }
                }
            }
        }

        public static void PretifyJson(string text, Action<string> setText)
        {
            // prettify the Json (and convert to Json if it wasn't already)
            f3.Resource resource = null;
            string contentAsJson;
            try
            {
                if (text.StartsWith("{"))
                {
                    contentAsJson = text;
                }
                else
                {
                    resource = new stu3.Hl7.Fhir.Serialization.FhirXmlParser().Parse<f3.Resource>(text);
                    contentAsJson = new stu3.Hl7.Fhir.Serialization.FhirJsonSerializer().SerializeToString(resource);
                }
                var sr = new System.IO.StringReader(contentAsJson);
                var reader = new JsonTextReader(sr);
                var doc = JObject.Load(reader);
                setText(doc.ToString(Formatting.Indented));
            }
            catch (Exception)
            {
                f2.Resource resource2 = null;
                try
                {
                    if (text.StartsWith("{"))
                    {
                        contentAsJson = text;
                    }
                    else
                    {
                        resource2 = new dstu2.Hl7.Fhir.Serialization.FhirXmlParser().Parse<f2.Resource>(text);
                        contentAsJson = new dstu2.Hl7.Fhir.Serialization.FhirJsonSerializer().SerializeToString(resource2);
                    }
                    var sr = new System.IO.StringReader(contentAsJson);
                    var reader = new JsonTextReader(sr);
                    var doc = JObject.Load(reader);
                    setText(doc.ToString(Formatting.Indented));
                }
                catch (Exception)
                {
                    f4.Resource resource4 = null;
                    try
                    {
                        if (text.StartsWith("{"))
                        {
                            contentAsJson = text;
                        }
                        else
                        {
                            resource4 = new r4.Hl7.Fhir.Serialization.FhirXmlParser().Parse<f4.Resource>(text);
                            contentAsJson = new r4.Hl7.Fhir.Serialization.FhirJsonSerializer().SerializeToString(resource4);
                        }
                        var sr = new System.IO.StringReader(contentAsJson);
                        var reader = new JsonTextReader(sr);
                        var doc = JObject.Load(reader);
                        setText(doc.ToString(Formatting.Indented));
                    }
                    catch (Exception)
                    {
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
                        _cm4 = new List<r4.Hl7.Fhir.Introspection.ClassMapping>() { r4.Hl7.Fhir.Introspection.ClassMapping.Create(t4) };
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
                        _cm3 = new List<stu3.Hl7.Fhir.Introspection.ClassMapping>() { stu3.Hl7.Fhir.Introspection.ClassMapping.Create(t3) };
                    }
                    catch
                    {
                    }
                }
                var t2 = dstu2.Hl7.Fhir.Model.ModelInfo.GetTypeForFhirType(typeName);
                if (t2 != null)
                {
                    try
                    {
                        _cm2 = new List<dstu2.Hl7.Fhir.Introspection.ClassMapping>() { dstu2.Hl7.Fhir.Introspection.ClassMapping.Create(t2) };
                    }
                    catch
                    {
                    }
                }
            }
        }

        private ExpressionElementContext()
        {
            _cm4 = new List<r4.Hl7.Fhir.Introspection.ClassMapping>();
            _cm3 = new List<stu3.Hl7.Fhir.Introspection.ClassMapping>();
            _cm2 = new List<dstu2.Hl7.Fhir.Introspection.ClassMapping>();
        }

        // Questionnaire Context information when processing validation against a questionnaire
        internal f2.Questionnaire _q2;
        internal f3.Questionnaire _q3;
        internal f4.Questionnaire _q4;
        internal List<f2.Questionnaire.GroupComponent> _2gs;
        internal List<f2.Questionnaire.QuestionComponent> _2qs;
        internal List<f3.Questionnaire.ItemComponent> _3is;
        internal List<f4.Questionnaire.ItemComponent> _4is;

        // Where this is the context of a class
        string _typeName;
        List<r4.Hl7.Fhir.Introspection.ClassMapping> _cm4;
        List<stu3.Hl7.Fhir.Introspection.ClassMapping> _cm3;
        List<dstu2.Hl7.Fhir.Introspection.ClassMapping> _cm2;

        public string possibleTypes()
        {
            return string.Join(", ", _cm2.Select(t => "::" + t.Name).Union(_cm3.Select(t => "::" + t.Name)).Union(_cm4.Select(t => "::" + t.Name)));
        }

        bool IsMatchingPropName(string propertyName, r4::Hl7.Fhir.Introspection.PropertyMapping pm)
        {
            if (propertyName == pm.Name)
                return true;
            if (propertyName + "[x]" == pm.Name)
                return true;
            if (!pm.MatchesSuffixedName(propertyName))
                return false;
            string ExpectedType = propertyName.Substring(pm.Name.Length);
            if (Enum.TryParse<f4.FHIRAllTypes>(ExpectedType, out var result))
            {
                if (String.Compare(ExpectedType, Hl7.Fhir.Utility.EnumUtility.GetLiteral(result), true) != 0)
                    return false;
                if (f4.ModelInfo.IsDataType(result) || f4.ModelInfo.IsPrimitive(result))
                    return true;
            }
            return false;
        }

        bool IsMatchingPropName(string propertyName, stu3::Hl7.Fhir.Introspection.PropertyMapping pm)
        {
            if (propertyName == pm.Name)
                return true;
            if (propertyName + "[x]" == pm.Name)
                return true;
            if (!pm.MatchesSuffixedName(propertyName))
                return false;
            string ExpectedType = propertyName.Substring(pm.Name.Length);
            if (Enum.TryParse<f3.FHIRAllTypes>(ExpectedType, out var result))
            {
                if (String.Compare(ExpectedType, Hl7.Fhir.Utility.EnumUtility.GetLiteral(result), true) != 0)
                    return false;
                if (f3.ModelInfo.IsDataType(result) || f3.ModelInfo.IsPrimitive(result))
                    return true;
            }
            return false;
        }

        bool IsMatchingPropName(string propertyName, dstu2::Hl7.Fhir.Introspection.PropertyMapping pm)
        {
            if (propertyName == pm.Name)
                return true;
            if (propertyName + "[x]" == pm.Name)
                return true;
            if (!pm.MatchesSuffixedName(propertyName))
                return false;
            string ExpectedType = propertyName.Substring(pm.Name.Length);
            if (Enum.TryParse<f2.FHIRDefinedType>(ExpectedType, out var result))
            {
                if (String.Compare(ExpectedType, Hl7.Fhir.Utility.EnumUtility.GetLiteral(result), true) != 0)
                    return false;
                if (f2.ModelInfo.IsDataType(result) || f2.ModelInfo.IsPrimitive(result))
                    return true;
            }
            return false;
        }

        bool HasProperty(string propName,
                out List<dstu2.Hl7.Fhir.Introspection.PropertyMapping> dstu2,
                out List<stu3.Hl7.Fhir.Introspection.PropertyMapping> stu3,
                out List<r4.Hl7.Fhir.Introspection.PropertyMapping> r4)
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
            if (r4?.Any() == true)
            {
                foreach (var item in r4)
                {
                    try
                    {
                        if (item.Choice == r4::Hl7.Fhir.Introspection.ChoiceType.DatatypeChoice)
                        {
                            if (item.Name != propertyName && item.MatchesSuffixedName(propertyName))
                            {
                                string ExpectedType = propertyName.Substring(item.Name.Length);
                                if (Enum.TryParse<f4.FHIRAllTypes>(ExpectedType, out var result))
                                {
                                    if (f4.ModelInfo.IsDataType(result) || f4.ModelInfo.IsPrimitive(result))
                                    {
                                        // may need to recheck that a typename of value23 won't work here
                                        string name = f4.ModelInfo.FhirTypeToFhirTypeName(result);
                                        var t = f4.ModelInfo.GetTypeForFhirType(name);
                                        newContext._cm4.Add(r4::Hl7.Fhir.Introspection.ClassMapping.Create(t));
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
                                        newContext._cm4.Add(r4::Hl7.Fhir.Introspection.ClassMapping.Create(t));
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (item.ElementType != typeof(string)) // (only occurs for extension.url and elementdefinition.id)
                                newContext._cm4.Add(r4::Hl7.Fhir.Introspection.ClassMapping.Create(item.ElementType));
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
                        if (item.Choice == stu3::Hl7.Fhir.Introspection.ChoiceType.DatatypeChoice)
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
                                        newContext._cm3.Add(stu3::Hl7.Fhir.Introspection.ClassMapping.Create(t));
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
                                        newContext._cm3.Add(stu3::Hl7.Fhir.Introspection.ClassMapping.Create(t));
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (item.ImplementingType != typeof(string)) // (only occurs for extension.url and elementdefinition.id)
                                newContext._cm3.Add(stu3::Hl7.Fhir.Introspection.ClassMapping.Create(item.ImplementingType));
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
                        if (item.Choice == dstu2::Hl7.Fhir.Introspection.ChoiceType.DatatypeChoice)
                        {
                            if (item.Name != propertyName && item.MatchesSuffixedName(propertyName))
                            {
                                string ExpectedType = propertyName.Substring(item.Name.Length);
                                if (Enum.TryParse<f2.FHIRDefinedType>(ExpectedType, out var result))
                                {
                                    if (f2.ModelInfo.IsDataType(result) || f2.ModelInfo.IsPrimitive(result))
                                    {
                                        // may need to recheck that a typename of value23 won't work here
                                        string name = f2.ModelInfo.FhirTypeToFhirTypeName(result);
                                        var t = f2.ModelInfo.GetTypeForFhirType(name);
                                        newContext._cm2.Add(dstu2::Hl7.Fhir.Introspection.ClassMapping.Create(t));
                                    }
                                }
                            }
                            else
                            {
                                // would be great to be able to filter through only the viable types
                                // but we don't have that, so just enumerate the available datatypes
                                foreach (f2.FHIRDefinedType ev in Enum.GetValues(typeof(f2.FHIRDefinedType)))
                                {
                                    if (f2.ModelInfo.IsDataType(ev) && !f2.ModelInfo.IsCoreSuperType(ev))
                                    {
                                        string name = f2.ModelInfo.FhirTypeToFhirTypeName(ev);
                                        var t = f2.ModelInfo.GetTypeForFhirType(name);
                                        newContext._cm2.Add(dstu2::Hl7.Fhir.Introspection.ClassMapping.Create(t));
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (item.ElementType != typeof(string)) // (only occurs for extension.url and elementdefinition.id)
                                newContext._cm2.Add(dstu2::Hl7.Fhir.Introspection.ClassMapping.Create(item.ElementType));
                        }
                    }
                    catch
                    {
                    }
                }
            }
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
            if (Enum.TryParse<f2.FHIRDefinedType>(typeCast, out var result2))
            {
                if (f2.ModelInfo.IsDataType(result2) || f2.ModelInfo.IsPrimitive(result2))
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
