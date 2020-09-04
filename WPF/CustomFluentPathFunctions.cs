﻿extern alias dstu2;
extern alias stu3;
extern alias r4;
// https://github.com/NuGet/Home/issues/4989#issuecomment-311042085

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model.Primitives;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Utility;
// using Hl7.Fhir.FluentPath;
//using Hl7.Fhir.Model;
using Hl7.FhirPath;
using Hl7.FhirPath.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FhirPathTester
{
    public class CustomFluentPathFunctions
    {
        static private SymbolTable _st;
        static public SymbolTable Scope
        {
            get
            {
                if (_st == null)
                {
                    _st = new SymbolTable().AddStandardFP();
                    // _st.Add("rand", (object f) => { return "slim"; });

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
                            .SelectMany(word => ElementNode.CreateList(mp.BuildKey(word)) );
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
                    _st.Add("dateadd", (PartialDateTime f, string field, long amount) =>
                    {
                        DateTimeOffset dto = f.ToUniversalTime();
                        int value = (int)amount;

                        // Need to convert the amount and field to compensate for partials
                        //TimeSpan ts = new TimeSpan();

                        switch (field)
                        {
                            case "yy": dto = dto.AddYears(value); break;
                            case "mm": dto = dto.AddMonths(value); break;
                            case "dd": dto = dto.AddDays(value); break;
                            case "hh": dto = dto.AddHours(value); break;
                            case "mi": dto = dto.AddMinutes(value); break;
                            case "ss": dto = dto.AddSeconds(value); break;
                        }
                        PartialDateTime changedDate = PartialDateTime.Parse(PartialDateTime.FromDateTime(dto).ToString().Substring(0, f.ToString().Length));
                        return changedDate;
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
                    Hl7.Fhir.Model.Base fhirValue = ElementNodeExtensions.Annotation<r4::Hl7.Fhir.ElementModel.IFhirValueProvider>(this)?.FhirValue;
                    if (fhirValue == null)
                        fhirValue = ElementNodeExtensions.Annotation<stu3::Hl7.Fhir.ElementModel.IFhirValueProvider>(this)?.FhirValue;
                    else if (fhirValue == null)
                        fhirValue = ElementNodeExtensions.Annotation<dstu2::Hl7.Fhir.ElementModel.IFhirValueProvider>(this)?.FhirValue;
                    if (fhirValue is r4::Hl7.Fhir.Model.Identifier ident)
                    {
                        // Need to construct a where clause for this property
                        if (!string.IsNullOrEmpty(ident.System))
                            return $"{parentCommonPath}.{Current.Name}.where(system='{ident.System}')";
                    }
                    else if (fhirValue is r4::Hl7.Fhir.Model.ContactPoint cp)
                    {
                        // Need to construct a where clause for this property
                        if (cp.System.HasValue)
                            return $"{parentCommonPath}.{Current.Name}.where(system='{cp.System.Value.GetLiteral()}')";
                    }
                    else if (fhirValue is r4::Hl7.Fhir.Model.Coding cd)
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
                    else if (fhirValue is r4::Hl7.Fhir.Model.Extension ext4)
                    {
                        // Need to construct a where clause for this property
                        // The extension is different as with fhirpath there
                        // is a shortcut format of .extension('url'), and since
                        // all extensions have a property name of extension, can just at the brackets and string name
                        return $"{parentCommonPath}.{Current.Name}('{ext4.Url}')";
                    }
                    else if (fhirValue is stu3::Hl7.Fhir.Model.Extension ext3)
                    {
                        // Need to construct a where clause for this property
                        // The extension is different as with fhirpath there
                        // is a shortcut format of .extension('url'), and since
                        // all extensions have a property name of extension, can just at the brackets and string name
                        return $"{parentCommonPath}.{Current.Name}('{ext3.Url}')";
                    }
                    else if (fhirValue is dstu2::Hl7.Fhir.Model.Extension ext2)
                    {
                        // Need to construct a where clause for this property
                        // The extension is different as with fhirpath there
                        // is a shortcut format of .extension('url'), and since
                        // all extensions have a property name of extension, can just at the brackets and string name
                        return $"{parentCommonPath}.{Current.Name}('{ext2.Url}')";
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
}
