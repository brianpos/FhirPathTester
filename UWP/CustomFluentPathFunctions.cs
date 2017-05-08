// extern alias dstu2;
// extern alias stu3;
// https://github.com/dotnet/cli/issues/564

using Hl7.Fhir.ElementModel;
// using Hl7.Fhir.FluentPath;
//using Hl7.Fhir.Model;
using Hl7.FhirPath;
using Hl7.FhirPath.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;

using fp2 = Hl7.Fhir.FhirPath;
using f2 = Hl7.Fhir.Model;
using s2 = Hl7.Fhir.Serialization;

using fp3 = Hl7.Fhir.FhirPath;
using f3 = Hl7.Fhir.Model;
using s3 = Hl7.Fhir.Serialization;
using Hl7.Fhir.Model.Primitives;

namespace FhirPathTesterUWP
{
    public static class CustomExtensions
    {
        public static IEnumerable<f2.Base> ToFhirValuesDSTU2(this IEnumerable<IElementNavigator> results)
        {
            return results.Select(r =>
            {
                if (r == null)
                    return null;

                if (r is fp2.PocoNavigator && (r as fp2.PocoNavigator).FhirValue != null)
                {
                    return ((fp2.PocoNavigator)r).FhirValue;
                }
                object result;
                if (r.Value is Hl7.FhirPath.ConstantValue)
                {
                    result = (r.Value as Hl7.FhirPath.ConstantValue).Value;
                }
                else
                {
                    result = r.Value;
                }

                if (result is bool)
                {
                    return new f2.FhirBoolean((bool)result);
                }
                if (result is long)
                {
                    return new f2.Integer((int)(long)result);
                }
                if (result is decimal)
                {
                    return new f2.FhirDecimal((decimal)result);
                }
                if (result is string)
                {
                    return new f2.FhirString((string)result);
                }
                if (result is PartialDateTime)
                {
                    var dt = (PartialDateTime)result;
                    return new f2.FhirDateTime(dt.ToUniversalTime());
                }
                else
                {
                    // This will throw an exception if the type isn't one of the FHIR types!
                    return (f2.Base)result;
                }
            });
        }
        public static IEnumerable<f3.Base> ToFhirValuesSTU3(this IEnumerable<IElementNavigator> results)
        {
            return results.Select(r =>
            {
                if (r == null)
                    return null;

                if (r is fp3.PocoNavigator && (r as fp3.PocoNavigator).FhirValue != null)
                {
                    return ((fp3.PocoNavigator)r).FhirValue;
                }
                object result;
                if (r.Value is Hl7.FhirPath.ConstantValue)
                {
                    result = (r.Value as Hl7.FhirPath.ConstantValue).Value;
                }
                else
                {
                    result = r.Value;
                }

                if (result is bool)
                {
                    return new f3.FhirBoolean((bool)result);
                }
                if (result is long)
                {
                    return new f3.Integer((int)(long)result);
                }
                if (result is decimal)
                {
                    return new f3.FhirDecimal((decimal)result);
                }
                if (result is string)
                {
                    return new f3.FhirString((string)result);
                }
                if (result is PartialDateTime)
                {
                    var dt = (PartialDateTime)result;
                    return new f3.FhirDateTime(dt.ToUniversalTime());
                }
                else
                {
                    // This will throw an exception if the type isn't one of the FHIR types!
                    return (f3.Base)result;
                }
            });
        }
    }
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
                        if (f is IEnumerable<IElementNavigator>)
                        {
                            object[] bits = (f as IEnumerable<IElementNavigator>).Select(i =>
                            {
                                if (i is fp3.PocoNavigator)
                                {
                                    return (i as fp3.PocoNavigator).Name;
                                }
                                if (i is fp2.PocoNavigator)
                                {
                                    return (i as fp2.PocoNavigator).Name;
                                }
                                return "?";
                            }).ToArray();
                            return FhirValueList.Create(bits);
                        }
                        return FhirValueList.Create(new object[] { "?" });
                    });
                    _st.Add("pathname", (object f) =>
                    {
                        if (f is IEnumerable<IElementNavigator>)
                        {
                            object[] bits = (f as IEnumerable<IElementNavigator>).Select(i =>
                            {
                                if (i is fp3.PocoNavigator)
                                {
                                    return (i as fp3.PocoNavigator).Path;
                                }
                                if (i is fp2.PocoNavigator)
                                {
                                    return (i as fp2.PocoNavigator).Path;
                                }
                                return "?";
                            }).ToArray();
                            return FhirValueList.Create(bits);
                        }
                        return FhirValueList.Create(new object[] { "?" });
                    });
                    _st.Add("shortpathname", (object f) =>
                    {
                        if (f is IEnumerable<IElementNavigator>)
                        {
                            object[] bits = (f as IEnumerable<IElementNavigator>).Select(i =>
                            {
                                if (i is fp3.PocoNavigator)
                                {
                                    return (i as fp3.PocoNavigator).ShortPath;
                                }
                                if (i is fp2.PocoNavigator)
                                {
                                    return (i as fp2.PocoNavigator).ShortPath;
                                }
                                return "?";
                            }).ToArray();
                            return FhirValueList.Create(bits);
                        }
                        return FhirValueList.Create(new object[] { "?" });
                    });
                    _st.Add("commonpathname", (object f) =>
                    {
                        if (f is IEnumerable<IElementNavigator>)
                        {
                            object[] bits = (f as IEnumerable<IElementNavigator>).Select(i =>
                            {
                                if (i is fp3.PocoNavigator)
                                {
                                    return (i as fp3.PocoNavigator).CommonPath;
                                }
                                if (i is fp2.PocoNavigator)
                                {
                                    return (i as fp2.PocoNavigator).CommonPath;
                                }
                                return "?";
                            }).ToArray();
                            return FhirValueList.Create(bits);
                        }
                        return FhirValueList.Create(new object[] { "?" });
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
                }
                return _st;
            }
        }

    }
}
