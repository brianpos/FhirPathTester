extern alias r5;
extern alias stu3;
extern alias r4;
// https://github.com/NuGet/Home/issues/4989#issuecomment-311042085

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Utility;
using Hl7.FhirPath;
using Hl7.FhirPath.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using P = Hl7.Fhir.ElementModel.Types;

namespace FhirPathTester
{
    public class CustomFluentPathFunctions
    {
        static public P.Date Add(P.Date me, P.Quantity value)
        {
            var result = me;
            // me.Years;

            return result;
        }

        static private SymbolTable _st;
        static public SymbolTable Scope
        {
            get
            {
                if (_st == null)
                {
                    _st = new SymbolTable().AddStandardFP();
                    // _st.Add("rand", (object f) => { return "slim"; });

                    _st.Add("binary.+", (object f, P.Date a, P.Quantity b) => Add(a, b), doNullProp: true);

                    // Custom function that returns the name of the property, rather than its value
                    _st.Add("propname", (object f) =>
                    {
                        if (f is IEnumerable<ITypedElement>)
                        {
                            object[] bits = (f as IEnumerable<ITypedElement>).Select(i =>
                            {
                                return i.Name;
                            }).ToArray();
                            return FhirValueListCreate(bits);
                        }
                        return ElementNode.CreateList("?");
                    });
                    _st.Add("pathname", (object f) =>
                    {
                        if (f is IEnumerable<ITypedElement>)
                        {
                            object[] bits = (f as IEnumerable<ITypedElement>).Select(i =>
                            {
                                return i.Location;
                            }).ToArray();
                            return FhirValueListCreate(bits);
                        }
                        return ElementNode.CreateList("?");
                    });
                    _st.Add("shortpathname", (object f) =>
                    {
                        if (f is IEnumerable<ITypedElement>)
                        {
                            var bits = (f as IEnumerable<ITypedElement>).Select(i =>
                            {
                                if (i is IShortPathGenerator spg)
                                {
                                    return spg.ShortPath;
                                }
                                return "?";
                            });
                            return ElementNode.CreateList(bits);
                        }
                        return ElementNode.CreateList("?");
                    });
                    _st.Add("DoubleMetaphone", (IEnumerable<ITypedElement> f) =>
                    {
                        var mp = new Phonix.DoubleMetaphone(10);
                        return f.Select(it => it.Value as string)
                            .Where(s => !string.IsNullOrEmpty(s))
                            .SelectMany(s => System.Text.RegularExpressions.Regex.Split(s, @"\s+"))
                            .SelectMany(word => ElementNode.CreateList(mp.BuildKey(word)));
                    });
                    _st.Add("Metaphone", (IEnumerable<ITypedElement> f) =>
                    {
                        var mp = new Phonix.Metaphone(10);
                        return f.Select(it => it.Value as string)
                            .Where(s => !string.IsNullOrEmpty(s))
                            .SelectMany(s => System.Text.RegularExpressions.Regex.Split(s, @"\s+"))
                            .SelectMany(word => ElementNode.CreateList(mp.BuildKey(word)));
                    });
                    _st.Add("Stem", (IEnumerable<ITypedElement> f) =>
                    {
                        var stemmer = new Porter2Stemmer.EnglishPorter2Stemmer();
                        return f.Select(it => it.Value as string)
                            .Where(s => !string.IsNullOrEmpty(s))
                            .SelectMany(s => System.Text.RegularExpressions.Regex.Split(s, @"\s+"))
                            .SelectMany(word => ElementNode.CreateList(stemmer.Stem(word).Value));
                    });
                    _st.Add("Fuzzy", (IEnumerable<ITypedElement> f) =>
                    {
                        var stemmer = new Porter2Stemmer.EnglishPorter2Stemmer();
                        var mp = new Phonix.DoubleMetaphone(10);
                        return f.Select(it => it.Value as string)
                            .Where(s => !string.IsNullOrEmpty(s))
                            .Select(s => FuzzyTrim(s))
                            .SelectMany(s => System.Text.RegularExpressions.Regex.Split(s, @"\s+"))
                            .Take(4)
                            .Select(word => stemmer.Stem(word).Value)
                            .SelectMany(wordStem => ElementNode.CreateList(mp.BuildKey(wordStem)));
                    });
                    _st.Add("commonpathname", (object f) =>
                    {
                        if (f is IEnumerable<ITypedElement>)
                        {
                            return (f as IEnumerable<ITypedElement>).Select(i =>
                            {
                                if (i is SqlonfhirScopedNode sn)
                                {
                                    return ElementNode.ForPrimitive(sn.CommonPath);
                                }
                                // Fall-back to the ShortPath if it wasn't one of these
                                return ElementNode.ForPrimitive(i.Annotation<IShortPathGenerator>().ShortPath);
                            });
                        }
                        return ElementNode.CreateList("?");
                    });

                    // Custom function for evaluating the date operation (custom Healthconnex)
                    _st.Add("dateadd", (Hl7.Fhir.ElementModel.Types.DateTime me, string field, long amount) =>
                    {
                        DateTimeOffset dto = me.ToDateTimeOffset(DateTimeOffset.Now.Offset);
                        int value = (int)amount;

                        switch (field)
                        {
                            case "yy": dto = dto.AddYears(value); break;
                            case "mm": dto = dto.AddMonths(value); break;
                            case "dd": dto = dto.AddDays(value); break;
                            case "hh": dto = dto.AddHours(value); break;
                            case "mi": dto = dto.AddMinutes(value); break;
                            case "ss": dto = dto.AddSeconds(value); break;
                        }

                        string representation = dto.ToString(Hl7.Fhir.ElementModel.Types.DateTime.FMT_FULL);
                        if (representation.Length > me.ToString().Length)
                        {
                            // need to trim appropriately.
                            if (me.Precision <= Hl7.Fhir.ElementModel.Types.DateTimePrecision.Minute)
                                representation = representation.Substring(0, me.ToString().Length);
                            else
                            {
                                if (!me.HasOffset)
                                {
                                    // trim the offset from it
                                    representation = dto.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF");
                                }
                            }
                        }

                        Hl7.Fhir.ElementModel.Types.DateTime changedDate;
                        if (Hl7.Fhir.ElementModel.Types.DateTime.TryParse(representation, out changedDate))
                            return changedDate;
                        return null;
                    });
                    _st.Add("dateadd", (Hl7.Fhir.ElementModel.Types.Date me, string field, long amount) =>
                    {
                        DateTimeOffset dto = me.ToDateTime().ToDateTimeOffset(DateTimeOffset.Now.Offset);
                        int value = (int)amount;

                        switch (field)
                        {
                            case "yy": dto = dto.AddYears(value); break;
                            case "mm": dto = dto.AddMonths(value); break;
                            case "dd": dto = dto.AddDays(value); break;
                            case "hh": dto = dto.AddHours(value); break;
                            case "mi": dto = dto.AddMinutes(value); break;
                            case "ss": dto = dto.AddSeconds(value); break;
                        }

                        string representation = dto.ToString(Hl7.Fhir.ElementModel.Types.DateTime.FMT_FULL);
                        if (representation.Length > me.ToString().Length)
                        {
                            // need to trim appropriately.
                            representation = representation.Substring(0, me.ToString().Length);
                        }

                        Hl7.Fhir.ElementModel.Types.Date changedDate;
                        if (Hl7.Fhir.ElementModel.Types.Date.TryParse(representation, out changedDate))
                            return changedDate;
                        return null;
                    });

                    // Custom LUHN Checksum algorithm
                    // https://rosettacode.org/wiki/Luhn_test_of_credit_card_numbers#C.23
                    _st.Add("LuhnTest", (string str) =>
                    {
                        return str.LuhnCheck();
                    });
                }
                return _st;
            }
        }

        static public string FuzzyTrim(string word)
        {
            if (string.IsNullOrEmpty(word))
                return null;
            return word.Replace("'", "").Replace("\"", "").Replace("`", "")
                                        .Replace(",", " ").Replace("/", " ").Replace("-", " ").Replace(".", " ").Replace("~", " ").Replace("?", " ")
                                        .Replace(";", " ").Replace("&", " ").Replace("*", " ").Replace("(", " ").Replace(")", " ").Replace("_", " ");
        }

        private static object FhirValueListCreate(object[] values)
        {
            return ElementNode.CreateList(values);
        }
    }

    public static class Luhn
    {
        public static bool LuhnCheck(this string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber))
                return true;
            return LuhnCheck(cardNumber.Select(c => c - '0').ToArray());
        }

        private static bool LuhnCheck(this int[] digits)
        {
            return GetCheckValue(digits) == 0;
        }

        private static int GetCheckValue(int[] digits)
        {
            return digits.Select((d, i) => i % 2 == digits.Length % 2 ? ((2 * d) % 10) + d / 5 : d).Sum() % 10;
        }
    }

    public class SqlonfhirScopedNode : ITypedElement, IAnnotated
    {
        public readonly ITypedElement Current = null;

        public SqlonfhirScopedNode(ITypedElement wrapped)
        {
            Current = wrapped;
        }

        private SqlonfhirScopedNode(SqlonfhirScopedNode parent, ITypedElement wrapped)
        {
            Current = wrapped;
            Parent = parent;
        }

        /// <summary>
        /// Represents the most direct parent in which the current node 
        /// is located.
        /// </summary>
        /// <remarks>
        /// When the node is the initial root, there is no parent.
        /// </remarks>
        public readonly SqlonfhirScopedNode Parent;

        public string CommonPath
        {
            get
            {
                // return ShortPath;
                string parentCommonPath = Parent?.CommonPath;
                if (String.IsNullOrEmpty(parentCommonPath))
                {
                    return Current.Name;
                }
                // Needs to consider that the index might be irrelevant
                if (ShortPath.EndsWith("]"))
                {
                    Hl7.Fhir.Model.Base fhirValue = ElementNodeExtensions.Annotation<Hl7.Fhir.ElementModel.IFhirValueProvider>(this)?.FhirValue;
                    if (fhirValue is Hl7.Fhir.Model.Identifier ident)
                    {
                        // Need to construct a where clause for this property
                        if (!string.IsNullOrEmpty(ident.System))
                            return $"{parentCommonPath}.{Current.Name}.where(system='{ident.System}')";
                    }
                    else if (fhirValue is Hl7.Fhir.Model.ContactPoint cp)
                    {
                        // Need to construct a where clause for this property
                        if (cp.System.HasValue)
                            return $"{parentCommonPath}.{Current.Name}.where(system='{cp.System.Value.GetLiteral()}')";
                    }
                    else if (fhirValue is Hl7.Fhir.Model.Coding cd)
                    {
                        // Need to construct a where clause for this property
                        if (!string.IsNullOrEmpty(cd.System))
                            return $"{parentCommonPath}.{Current.Name}.where(system='{cd.System}')";
                    }
                    else if (fhirValue is r4::Hl7.Fhir.Model.Address addr)
                    {
                        // Need to construct a where clause for this property
                        if (addr.Use.HasValue)
                            return $"{parentCommonPath}.{Current.Name}.where(use='{addr.Use.Value.GetLiteral()}')";
                    }
                    else if (fhirValue is r4::Hl7.Fhir.Model.Questionnaire.ItemComponent gc)
                    {
                        // Need to construct a where clause for this property
                        if (!string.IsNullOrEmpty(gc.LinkId))
                            return $"{parentCommonPath}.{Current.Name}.where(linkId='{gc.LinkId}')";
                    }
                    else if (fhirValue is r4::Hl7.Fhir.Model.QuestionnaireResponse.ItemComponent rgc)
                    {
                        // Need to construct a where clause for this property
                        if (!string.IsNullOrEmpty(rgc.LinkId))
                            return $"{parentCommonPath}.{Current.Name}.where(linkId='{rgc.LinkId}')";
                    }
                    else if (fhirValue is Hl7.Fhir.Model.Extension ext4)
                    {
                        // Need to construct a where clause for this property
                        // The extension is different as with fhirpath there
                        // is a shortcut format of .extension('url'), and since
                        // all extensions have a property name of extension, can just at the brackets and string name
                        return $"{parentCommonPath}.{Current.Name}('{ext4.Url}')";
                    }
                    return $"{parentCommonPath}.{LocalLocation}";
                }
                return $"{Parent.CommonPath}.{Current.Name}";
            }
        }

        public string ShortPath
        {
            get
            {
                return ElementNodeExtensions.Annotation<IShortPathGenerator>(this)?.ShortPath;
            }
        }

        public string LocalLocation => Parent == null ? Location :
                        $"{Location.Substring(Parent.Location.Length + 1)}";

        #region << ITypedElement members >>
        public string Name => Current.Name;

        public string InstanceType => Current.InstanceType;

        public object Value => Current.Value;

        public string Location => Current.Location;

        public IElementDefinitionSummary Definition => Current.Definition;

        public IEnumerable<ITypedElement> Children(string name = null) =>
            Current.Children(name).Select(c => new SqlonfhirScopedNode(this, c));
        #endregion

        #region << IAnnotated members >>
        public IEnumerable<object> Annotations(Type type)
        {
            if (type == typeof(SqlonfhirScopedNode))
                return new[] { this };
            else
                return Current.Annotations(type);
        }
        #endregion
    }

    public class NonFhirJSonNode : ITypedElement //, IAnnotated
    {
        public NonFhirJSonNode(Newtonsoft.Json.Linq.JObject value)
        {
            _value = value;
        }

        private NonFhirJSonNode(NonFhirJSonNode parent, string propName, Newtonsoft.Json.Linq.JToken value)
        {
            _propName = propName;
            _parent = parent;
            _value = value;
        }

        NonFhirJSonNode _parent;
        Newtonsoft.Json.Linq.JToken _value;
        string _propName;

        #region << ITypedElement members >>
        public string Name
        {
            get
            {
                if (_propName?.Contains("[") == true)
                {
                    return _propName.Substring(0, _propName.IndexOf("["));
                }
                return _propName;
            }
        }

        public string InstanceType
        {
            get
            {
                if (_value is Newtonsoft.Json.Linq.JObject jo)
                {
                    return jo.Type.ToString();
                }
                if (_value is Newtonsoft.Json.Linq.JProperty jp)
                {
                    return jp.Value.Type.ToString();
                }
                return _value.Type.ToString();
            }
        }

        public object Value
        {
            get
            {
                if (_value is Newtonsoft.Json.Linq.JValue jv)
                    return jv.Value;
                if (_value is Newtonsoft.Json.Linq.JProperty jp)
                    return jp.Value;
                return _value;
            }
        }

        public string Location
        {
            get
            {
                if (!string.IsNullOrEmpty(_parent?.Location) && _parent?.Location != "root")
                    return _parent.Location + "." + _propName;
                return _propName ?? "root";
            }
        }

        public IElementDefinitionSummary Definition => throw new NotImplementedException();

        private static IEnumerable<ITypedElement> Children(NonFhirJSonNode parent, Newtonsoft.Json.Linq.JToken value, string name)
        {
            if (value is Newtonsoft.Json.Linq.JProperty jp)
            {
                if (!string.IsNullOrEmpty(name) && name != jp.Name)
                    yield break;
                foreach (var propValue in Children(parent, jp.Value, name))
                    yield return propValue;
            }
            else if (value is Newtonsoft.Json.Linq.JValue jv)
            {
                if (jv.Parent is Newtonsoft.Json.Linq.JProperty jp2)
                {
                    if (!string.IsNullOrEmpty(name) && name != jp2.Name)
                        yield break;
                    yield return new NonFhirJSonNode(parent, jp2.Name, value);
                }
            }
            else if (value is Newtonsoft.Json.Linq.JArray ja)
            {
                if (ja.Parent is Newtonsoft.Json.Linq.JProperty jp2)
                {
                    if (!string.IsNullOrEmpty(name) && name != jp2.Name)
                        yield break;
                    for (int n = 0; n < ja.Count; n++)
                    {
                        var arrayItem = ja[n];
                        yield return new NonFhirJSonNode(parent, $"{jp2.Name}[{n}]", arrayItem);
                    }
                }
            }
            else if (value is Newtonsoft.Json.Linq.JObject jo)
            {
                foreach (var p in jo.Properties())
                {
                    foreach (var propValue in Children(parent, p, name))
                        yield return propValue;
                }
            }
        }

        public IEnumerable<ITypedElement> Children(string name = null)
        {
            return Children(this, _value, name);
            //var result = Children(this, _value, name).ToList();
            //return result;
        }
        #endregion
    }

    public class NonFhirXmlNode : ITypedElement
    {
        public NonFhirXmlNode(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            _value = doc.DocumentElement;
        }

        private NonFhirXmlNode(NonFhirXmlNode parent, string propName, XmlElement value)
        {
            _propName = propName;
            _parent = parent;
            _value = value;
        }

        string _propName;
        NonFhirXmlNode _parent;
        public XmlElement _value;

        #region << ITypedElement members >>
        public string Name => _value.Name;

        public string InstanceType => "String"; // unless we get a DSD to get type data from

        public object Value => _value.GetAttribute("value");

        public string Location
        {
            get
            {
                if (!string.IsNullOrEmpty(_parent?.Location) && _parent?.Location != "root")
                    return _parent.Location + "." + _propName;
                return _propName ?? Name;
            }
        }

        public IElementDefinitionSummary Definition => throw new NotImplementedException();

        public IEnumerable<ITypedElement> Children(string name = null)
        {
            // return Children(this, _value, name);
            var result = Children(this, _value, name).ToList();
            return result;
        }

        private IEnumerable<ITypedElement> Children(NonFhirXmlNode nonFhirXmlNode, XmlElement value, string name)
        {
            string lastPropName = null;
            int arrayIndex = 0;
            foreach (XmlNode child in value.ChildNodes)
            {
                if (child.Name == lastPropName)
                    arrayIndex++;
                else
                {
                    lastPropName = child.Name;
                    arrayIndex = 0;
                }
                if (!string.IsNullOrEmpty(name) && child.Name != name)
                    continue;
                if (child is XmlElement xe)
                    yield return new NonFhirXmlNode(this, $"{child.Name}[{arrayIndex}]", xe);
            }
        }
        #endregion
    }
}
