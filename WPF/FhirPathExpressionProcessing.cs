extern alias stu3;
// using Hl7.FhirPath.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace FhirPathTester
{
    public class QuestionnaireExpressionProcessing
    {
        private Expression CheckExpression(Hl7.FhirPath.Expressions.Expression expr, Type T, List<string> namedProps)
        {
            if (expr is Hl7.FhirPath.Expressions.ChildExpression ce)
            {
                var focusContext = CheckExpression(ce.Focus, T, namedProps);
                var cm = stu3.Hl7.Fhir.Introspection.ClassMapping.Create(T);
                var childContext = Expression.Property(focusContext, cm.FindMappedElementByName(ce.ChildName).Name);
                if (childContext != null)
                    return childContext;
                throw new ArgumentException("Failed to find parameter", ce.ChildName);
            }
            if (expr is Hl7.FhirPath.Expressions.FunctionCallExpression func)
            {
                var funcs = _compiler.Symbols.Filter(func.FunctionName, func.Arguments.Count() + 1);
                if (funcs.Count() == 0 && !(expr is Hl7.FhirPath.Expressions.BinaryExpression))
                {
                    // AppendResults($"{prefix}{func.FunctionName} *invalid function name*", true);
                }
                else
                {
                    AppendResults($"{prefix}{func.FunctionName}");
                }
                var focusContext = CheckExpression(func.Focus, context);

                if (func.FunctionName == "binary.as")
                {
                    if (func.Arguments.Count() != 2)
                    {
                        AppendResults($"{prefix}{func.FunctionName} INVALID AS Operation", true);
                        return focusContext;
                    }
                    var argContextResult = CheckExpression(func.Arguments.First(), focusContext);
                    var typeArg = func.Arguments.Skip(1).FirstOrDefault() as Hl7.FhirPath.Expressions.ConstantExpression;
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
                    if (func.FunctionName == "where" && func.Arguments.Count() == 1 && (func.Arguments.First() as Hl7.FhirPath.Expressions.BinaryExpression)?.Op == "=")
                    {
                        var op = func.Arguments.First() as Hl7.FhirPath.Expressions.BinaryExpression;
                        var argContextResult = CheckExpression(op, focusContext);

                        // Filter the values that are not in this set
                        focusContext._2gs = argContextResult._2gs;
                        focusContext._2qs = argContextResult._2qs;
                    }
                    else
                    {
                        foreach (var item in func.Arguments)
                        {
                            var argContextResult = CheckExpression(item, focusContext);
                        }
                    }
                    if (func.FunctionName == "binary.=")
                    {
                        Hl7.FhirPath.Expressions.ChildExpression prop = (Hl7.FhirPath.Expressions.ChildExpression)func.Arguments.Where(a => a is Hl7.FhirPath.Expressions.ChildExpression).FirstOrDefault();
                        Hl7.FhirPath.Expressions.ConstantExpression value = (Hl7.FhirPath.Expressions.ConstantExpression)func.Arguments.Where(a => a is Hl7.FhirPath.Expressions.ConstantExpression).FirstOrDefault();
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
            else if (expr is Hl7.FhirPath.Expressions.ConstantExpression constExpr)
            {
                return Expression.Constant(constExpr.Value); // context doesn't propagate from this
            }
            else if (expr is Hl7.FhirPath.Expressions.VariableRefExpression vref)
            {
                // sb.AppendFormat("{0}{1} (variable ref)\r\n", prefix, func.Name);
                return context;
            }
            // AppendResults(expr.GetType().ToString());
            return context;
        }

    }
}
