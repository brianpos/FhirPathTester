extern alias dstu2;
extern alias stu3;
// https://github.com/NuGet/Home/issues/4989#issuecomment-311042085

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model.Primitives;
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
                        return FhirValueListCreate(new object[] { "?" } );
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
                        return FhirValueListCreate(new object[] { "?" });
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
                        return FhirValueListCreate(new object[] { "?" });
                    });
                    //_st.Add("DoubleMetaphone", (IEnumerable<ITypedElement> f) =>
                    //{
                    //    var mp = new Phonix.DoubleMetaphone(10);
                    //    return f.Select(it => it.Value as string)
                    //        .Where(s => !string.IsNullOrEmpty(s))
                    //        .SelectMany(s => System.Text.RegularExpressions.Regex.Split(s, @"\s+"))
                    //        .SelectMany(word => ElementNode.CreateList(mp.BuildKey(word)));
                    //});
                    //_st.Add("Metaphone", (IEnumerable<ITypedElement> f) =>
                    //{
                    //    var mp = new Phonix.Metaphone(10);
                    //    return f.Select(it => it.Value as string)
                    //        .Where(s => !string.IsNullOrEmpty(s))
                    //        .SelectMany(s => System.Text.RegularExpressions.Regex.Split(s, @"\s+"))
                    //        .SelectMany(word => ElementNode.CreateList(mp.BuildKey(word)));
                    //});
                    //_st.Add("Stem", (IEnumerable<ITypedElement> f) =>
                    //{
                    //    var stemmer = new Porter2Stemmer.EnglishPorter2Stemmer();
                    //    return f.Select(it => it.Value as string)
                    //        .Where(s => !string.IsNullOrEmpty(s))
                    //        .SelectMany(s => System.Text.RegularExpressions.Regex.Split(s, @"\s+"))
                    //        .SelectMany(word => ElementNode.CreateList(stemmer.Stem(word).Value));
                    //});
                    //_st.Add("Fuzzy", (IEnumerable<ITypedElement> f) =>
                    //{
                    //    var stemmer = new Porter2Stemmer.EnglishPorter2Stemmer();
                    //    var mp = new Phonix.DoubleMetaphone(10);
                    //    return f.Select(it => it.Value as string)
                    //        .Where(s => !string.IsNullOrEmpty(s))
                    //        .Select(s => FuzzyTrim(s))
                    //        .SelectMany(s => System.Text.RegularExpressions.Regex.Split(s, @"\s+"))
                    //        .Take(4)
                    //        .Select(word => stemmer.Stem(word).Value)
                    //        .SelectMany(wordStem => ElementNode.CreateList(mp.BuildKey(wordStem)));
                    //});
                    //_st.Add("commonpathname", (object f) =>
                    //{
                    //    if (f is IEnumerable<ITypedElement>)
                    //    {
                    //        object[] bits = (f as IEnumerable<ITypedElement>).Select(i =>
                    //        {
                    //            if (i is stu3.Hl7.Fhir.ElementModel.PocoElementNode)
                    //            {
                    //                return (i as stu3.Hl7.Fhir.ElementModel.PocoElementNode).CommonPath;
                    //            }
                    //            if (i is dstu2.Hl7.Fhir.ElementModel.PocoElementNode)
                    //            {
                    //                return (i as dstu2.Hl7.Fhir.ElementModel.PocoElementNode).CommonPath;
                    //            }
                    //            return "?";
                    //        }).ToArray();
                    //        return FhirValueListCreate(bits);
                    //    }
                    //    return FhirValueListCreate(new object[] { "?" });
                    //});

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
}
