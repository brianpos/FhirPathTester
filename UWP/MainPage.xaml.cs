extern alias dstu2;
extern alias stu3;
extern alias r4;
// https://github.com/NuGet/Home/issues/4989#issuecomment-311042085

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using Hl7.Fhir.ElementModel;

using fp2 = dstu2.Hl7.Fhir.FhirPath;
using f2 = dstu2.Hl7.Fhir.Model;
using s2 = dstu2.Hl7.Fhir.Serialization;

using fp3 = stu3.Hl7.Fhir.FhirPath;
using f3 = stu3.Hl7.Fhir.Model;
using s3 = stu3.Hl7.Fhir.Serialization;

using Hl7.FhirPath;
using Hl7.FhirPath.Expressions;
using System.Text;
using Windows.UI.Xaml.Documents;
using FhirPathTester;

namespace FhirPathTesterUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        public MainPage()
        {
            this.InitializeComponent();

            TextControlFontSize = 22;
            textboxInputXML.Text = "<Patient xmlns=\"http://hl7.org/fhir\">\r\n  <name>\r\n    <given value=\"brian\"/>\r\n  </name>\r\n  <birthDate value=\"1980\"/>\r\n</Patient>";
            textboxExpression.Text = "birthDate < today()";
            DataContext = this;

            var items = new List<MenuItem>();
            //items.Add(new MenuItem()
            //{
            //    Icon = Symbol.Accept,
            //    Name = "Accept"
            //});
            //items.Add(new MenuItem()
            //{
            //    Icon = Symbol.OutlineStar,
            //    Name = "Favourites"
            //});
            //items.Add(new MenuItem()
            //{
            //    Icon = Symbol.Calendar,
            //    Name = "Calendar"
            //});
            //items.Add(new MenuItem()
            //{
            //    Icon = Symbol.Bookmarks,
            //    Name = "Bookmarks"
            //});
            items.Add(new MenuItem()
            {
                Icon = Symbol.Home,
                Name = "Home"
            });
            //items.Add(new MenuItem()
            //{
            //    Icon = Symbol.Clock,
            //    Name = "History"
            //});
            //items.Add(new MenuItem()
            //{
            //    Icon = Symbol.Setting,
            //    Name = "Settings"
            //});
            hamburgerMenuControl.ItemsSource = items;
        }

        public class MenuItem
        {
            public Symbol Icon
            {
                get;
                set;
            }
            public string Name
            {
                get;
                set;
            }
        }

        private int _size;
        public int TextControlFontSize
        {
            get { return _size; }
            set
            {
                if (_size != value)
                {
                    _size = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TextControlFontSize)));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private IElementNavigator GetResourceNavigatorDSTU2(out string parseErrors)
        {
            f2.Resource resource = null;
            try
            {
                if (textboxInputXML.Text.StartsWith("{"))
                    resource = new s2.FhirJsonParser().Parse<f2.Resource>(textboxInputXML.Text);
                else
                    resource = new s2.FhirXmlParser().Parse<f2.Resource>(textboxInputXML.Text);
            }
            catch (Exception ex)
            {
                parseErrors = "DSTU2 Resource read error:\r\n" + ex.Message;
                return null;
            }
            parseErrors = null;
            var inputNav = new dstu2.Hl7.Fhir.ElementModel.PocoNavigator(resource);
            return inputNav;
        }
        private IElementNavigator GetResourceNavigatorSTU3(out string parseErrors)
        {
            f3.Resource resource = null;
            try
            {
                if (textboxInputXML.Text.StartsWith("{"))
                    resource = new s3.FhirJsonParser().Parse<f3.Resource>(textboxInputXML.Text);
                else
                    resource = new s3.FhirXmlParser().Parse<f3.Resource>(textboxInputXML.Text);
            }
            catch (Exception ex)
            {
                parseErrors = "STU3 Resource read error:\r\n" + ex.Message;
                return null;
            }
            parseErrors = null;
            var inputNav = new stu3.Hl7.Fhir.ElementModel.PocoNavigator(resource);
            return inputNav;
        }

        private IElementNavigator GetResourceNavigator()
        {
            string parseErrors2;
            var inputNavDSTU2 = GetResourceNavigatorDSTU2(out parseErrors2);
            string parseErrors3;
            var inputNavSTU3 = GetResourceNavigatorSTU3(out parseErrors3);

            if (!string.IsNullOrEmpty(parseErrors2) || !string.IsNullOrEmpty(parseErrors3))
            {
                ResetResults();
                // Windows.UI.Text.RichEditTextDocument
                if (!string.IsNullOrEmpty(parseErrors2))
                    AppendResults(parseErrors2, true);
                if (!string.IsNullOrEmpty(parseErrors2) && !string.IsNullOrEmpty(parseErrors3))
                    AppendResults("\r\n--------------------\r\n");
                if (!string.IsNullOrEmpty(parseErrors3))
                    AppendResults(parseErrors3, true);
            }

            if (inputNavSTU3 != null)
                labelSTU3.Visibility = Visibility.Visible;
            else
                labelSTU3.Visibility = Visibility.Collapsed;
            if (inputNavDSTU2 != null)
            {
                labelDSTU2.Visibility = Visibility.Visible;
                return inputNavDSTU2;
            }
            labelDSTU2.Visibility = Visibility.Collapsed;
            return inputNavSTU3;
        }

        private void ButtonGo_Click(object sender, RoutedEventArgs e)
        {
            var inputNav = GetResourceNavigator();
            if (inputNav == null)
                return;

            // Don't need to cache this, it is cached in the fhir-client
            CompiledExpression xps = null;
            try
            {
                xps = _compiler.Compile(textboxExpression.Text);
            }
            catch (Exception ex)
            {
                SetResults("Expression compilation error:\r\n" + ex.Message, true);
                return;
            }

            IEnumerable<IElementNavigator> prepopulatedValues = null;
            if (xps != null)
            {
                try
                {
                    if (inputNav is stu3.Hl7.Fhir.ElementModel.PocoNavigator)
                        prepopulatedValues = xps(inputNav, new fp3.FhirEvaluationContext(inputNav));
                    else
                        prepopulatedValues = xps(inputNav, new fp2.FhirEvaluationContext(inputNav));
                }
                catch (Exception ex)
                {
                    SetResults("Expression evaluation error:\r\n" + ex.Message);
                    AppendParseTree();
                    return;
                }

                ResetResults();
                try
                {
                    if (prepopulatedValues.Count() > 0)
                    {
                        string tooltip = null;
                        if (inputNav is stu3.Hl7.Fhir.ElementModel.PocoNavigator)
                        {
                            foreach (var t2 in fp3.ElementNavFhirExtensions.ToFhirValues(prepopulatedValues))
                            {
                                if (t2 != null)
                                {
                                    // output the content as XML fragments
                                    var fragment = s3.FhirSerializer.SerializeToXml(t2, root: t2.TypeName);
                                    AppendResults(fragment.Replace(" xmlns=\"http://hl7.org/fhir\"", ""));
                                }
                                // System.Diagnostics.Trace.WriteLine(string.Format("{0}: {1}", xpath.Value, t2.AsStringRepresentation()));
                            }
                        }
                        else if (inputNav is dstu2.Hl7.Fhir.ElementModel.PocoNavigator)
                        {

                            foreach (var t2 in fp2.ElementNavFhirExtensions.ToFhirValues(prepopulatedValues))
                            {
                                if (t2 != null)
                                {
                                    // output the content as XML fragments
                                    var fragment = s2.FhirSerializer.SerializeToXml(t2, root: t2.TypeName);
                                    AppendResults(fragment.Replace(" xmlns=\"http://hl7.org/fhir\"", ""), false, tooltip);
                                }
                                // System.Diagnostics.Trace.WriteLine(string.Format("{0}: {1}", xpath.Value, t2.AsStringRepresentation()));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    SetResults("Processing results error:\r\n" + ex.Message);
                    return;
                }
            }

            AppendParseTree();
        }

        private void AppendParseTree()
        {
            // Grab the parse expression
            StringBuilder sb = new StringBuilder();
            var expr = _compiler.Parse(textboxExpression.Text);
            OutputExpression(expr, sb, "");
            // textboxResult.Text += expr.Dump();
            AppendResults("\r\n\r\n----------------\r\n" + sb.ToString());
        }

        private void OutputExpression(Hl7.FhirPath.Expressions.Expression expr, StringBuilder sb, string prefix)
        {
            if (expr is ChildExpression)
            {
                var func = expr as ChildExpression;
                OutputExpression(func.Focus, sb, prefix + "-- ");
                sb.AppendFormat("{0}{1}\r\n", prefix, func.ChildName);
                return;
            }
            if (expr is FunctionCallExpression)
            {
                var func = expr as FunctionCallExpression;
                sb.AppendFormat("{0}{1}\r\n", prefix, func.FunctionName);
                OutputExpression(func.Focus, sb, prefix + "-- ");
                foreach (var item in func.Arguments)
                {
                    OutputExpression(item, sb, prefix + "    ");
                }
                return;
            }
            //else if (expr is BinaryExpression)
            //{
            //    var func = expr as BinaryExpression;
            //    sb.AppendLine(func.FunctionName);
            //    OutputExpression(func.Left, sb);
            //    sb.AppendLine(func.Op);
            //    OutputExpression(func.Right, sb);
            //    return;
            //}
            else if (expr is ConstantExpression)
            {
                var func = expr as ConstantExpression;
                sb.AppendFormat("{0}{1} (constant)\r\n", prefix, func.Value.ToString());
                return;
            }
            else if (expr is VariableRefExpression)
            {
                var func = expr as VariableRefExpression;
                // sb.AppendFormat("{0}{1} (variable ref)\r\n", prefix, func.Name);
                return;
            }
            sb.Append(expr.GetType().ToString());
        }

        Hl7.FhirPath.FhirPathCompiler _compiler = new FhirPathCompiler(CustomFluentPathFunctions.Scope);

        private void ButtonPredicate_Click(object sender, RoutedEventArgs e)
        {
            var inputNav = GetResourceNavigator();
            if (inputNav == null)
                return;

            // Don't need to cache this, it is cached in the fhir-client
            Hl7.FhirPath.CompiledExpression xps = null;
            try
            {
                xps = _compiler.Compile(textboxExpression.Text);
            }
            catch (Exception ex)
            {
                SetResults("Expression compilation error:\r\n" + ex.Message);
                return;
            }

            if (xps != null)
            {
                try
                {
                    bool result;
                    if (inputNav is stu3.Hl7.Fhir.ElementModel.PocoNavigator)
                        result = xps.Predicate(inputNav, new fp3.FhirEvaluationContext(inputNav));
                    else
                        result = xps.Predicate(inputNav, new fp2.FhirEvaluationContext(inputNav));
                    SetResults(result.ToString());
                }
                catch (Exception ex)
                {
                    SetResults("Expression evaluation error:\r\n" + ex.Message);
                    return;
                }
            }

            AppendParseTree();
        }

        private async void textboxInputXML_Drop(object sender, DragEventArgs e)
        {
            // This is the place where we want to support the reading of the file from the file system
            // to make the testing of other instances really easy
            var formats = e.Data.GetView().AvailableFormats;
            if (formats.Contains("FileName"))
            {
                string[] contents = await e.Data.GetView().GetDataAsync("FileName") as string[];
                if (contents.Length > 0)
                    textboxInputXML.Text = System.IO.File.ReadAllText(contents[0]);
                e.Handled = true;
            }
        }

        private void textboxInputXML_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy | Windows.ApplicationModel.DataTransfer.DataPackageOperation.Link;
        }

        private void textboxInputXML_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (TextControlFontSize > sliderFontSize.Minimum)
                TextControlFontSize--;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (TextControlFontSize < sliderFontSize.Maximum)
                TextControlFontSize++;
            // SmartAuthentication();
        }

        private void NotifyUser(string message)
        {
            labelStatus.Text = message;
        }

        private void ButtonCheckExpression_Click(object sender, RoutedEventArgs e)
        {
            var inputNav = GetResourceNavigator();
            if (inputNav == null)
                return;

            // Don't need to cache this, it is cached in the fhir-client
            Hl7.FhirPath.Expressions.Expression expr = null;
            try
            {
                expr = _compiler.Parse(textboxExpression.Text);
            }
            catch (Exception ex)
            {
                SetResults("Expression compilation error:\r\n" + ex.Message, true);
                return;
            }

            if (expr != null)
            {
                try
                {
                    ExpressionElementContext context = new ExpressionElementContext(inputNav.Name);
                    if (inputNav is dstu2::Hl7.Fhir.ElementModel.PocoNavigator pn2)
                    {
                        if (pn2.FhirValue is f2.Questionnaire q)
                        {
                            context._q2 = q;
                        }
                    }
                    else if (inputNav is stu3::Hl7.Fhir.ElementModel.PocoNavigator pn3)
                    {
                        if (pn3.FhirValue is f3.Questionnaire q)
                        {
                            context._q3 = q;
                        }
                    }
                    ResetResults();
                    CheckExpression(expr, "", context);
                }
                catch (Exception ex)
                {
                    SetResults("Expression Check error:\r\n" + ex.Message, true);
                    return;
                }
            }
        }

        private void ResetResults()
        {
            textboxResult.Blocks.Clear();
        }

        private void SetResults(string text, bool error = false)
        {
            textboxResult.Blocks.Clear();
            var run = new Run() { Text = text };
            var para = new Paragraph();
            para.Inlines.Add(run);
            textboxResult.Blocks.Add(para);
            if (error)
            {
                para.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Red);
                para.FontWeight = Windows.UI.Text.FontWeights.Bold;
            }
            else
            {
                para.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Black);
            }
        }
        private void AppendResults(string text, bool error = false, string tooltip = null)
        {
            var run = new Run() { Text = text };
            var para = new Paragraph();
            para.Inlines.Add(run);
            para.Margin = new Thickness(2);
            textboxResult.Blocks.Add(para);
            if (error)
            {
                para.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Red);
                para.FontWeight = Windows.UI.Text.FontWeights.Bold;
            }
            else
            {
                para.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Black);
            }
            if (!string.IsNullOrEmpty(tooltip))
            {
                ToolTip tip = new ToolTip();
                tip.Content = tooltip;
                ToolTipService.SetToolTip(para, tip);

                var run2 = new Run() { Text = " " + tooltip.Replace("\r\n", ", ") };
                run2.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Gray);
                run2.FontStyle = Windows.UI.Text.FontStyle.Italic;
                para.Inlines.Add(run2);
            }
        }

        public class ExpressionElementContext
        {
            public ExpressionElementContext(string typeName)
            {
                _typeName = typeName;
                if (!string.IsNullOrEmpty(typeName))
                {
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
                _cm3 = new List<stu3.Hl7.Fhir.Introspection.ClassMapping>();
                _cm2 = new List<dstu2.Hl7.Fhir.Introspection.ClassMapping>();
            }

            // Questionnaire Context information when processing validation against a questionnaire
            internal f2.Questionnaire _q2;
            internal f3.Questionnaire _q3;
            internal List<f2.Questionnaire.GroupComponent> _2gs;
            internal List<f2.Questionnaire.QuestionComponent> _2qs;
            internal List<f3.Questionnaire.ItemComponent> _3is;

            // Where this is the context of a class
            string _typeName;
            List<stu3.Hl7.Fhir.Introspection.ClassMapping> _cm3;
            List<dstu2.Hl7.Fhir.Introspection.ClassMapping> _cm2;

            public string possibleTypes()
            {
                return string.Join(", ", _cm2.Select(t => "::" + t.Name).Union(_cm3.Select(t => "::" + t.Name)));
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
                    out List<stu3.Hl7.Fhir.Introspection.PropertyMapping> stu3)
            {
                if (_cm3 == null)
                    stu3 = null;
                else
                    stu3 = _cm3.Select(t => t.FindMappedElementByName(propName)).Where(t => t != null && IsMatchingPropName(propName, t)).ToList();
                if (_cm2 == null)
                    dstu2 = null;
                else
                    dstu2 = _cm2.Select(t => t.FindMappedElementByName(propName)).Where(t => t != null && IsMatchingPropName(propName, t)).ToList();
                return (dstu2?.Count() > 0) || (stu3?.Count() > 0);
            }

            public ExpressionElementContext Child(string propertyName)
            {
                // Special case for the top level node
                if (propertyName == _typeName)
                    return this;

                if (!HasProperty(propertyName, out var dstu2, out var stu3))
                    return null;

                var newContext = new ExpressionElementContext();
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
                                if (item.ElementType != typeof(string)) // (only occurs for extension.url and elementdefinition.id)
                                    newContext._cm3.Add(stu3::Hl7.Fhir.Introspection.ClassMapping.Create(item.ElementType));
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
                if (_cm3?.Any() == true)
                {
                    sb.Append("STU3:");
                    foreach (var i3 in _cm3)
                    {
                        sb.Append($" {i3.Name}");
                    }
                }
                if (_cm2?.Any() == true)
                {
                    if (_cm3?.Any() == true)
                        sb.AppendLine();
                    sb.Append("DSTU2:");
                    foreach (var i2 in _cm2)
                    {
                        sb.Append($" {i2.Name}");
                    }
                }
                var groupLinkIds = _2gs?.Select(i => i.LinkId).ToArray();
                var questionLinkIds = _2qs?.Select(i => i.LinkId).ToArray();
                if (groupLinkIds != null)
                    sb.Append($"\r\nGroup LinkIds: {String.Join(", ", groupLinkIds)}");
                if (questionLinkIds != null)
                    sb.Append($"\r\nQuestion LinkIds: {String.Join(", ", questionLinkIds)}");
                return sb.ToString();
            }
        }

        private ExpressionElementContext CheckExpression(Hl7.FhirPath.Expressions.Expression expr, string prefix, ExpressionElementContext context)
        {
            if (expr is ChildExpression)
            {
                var func = expr as ChildExpression;
                var focusContext = CheckExpression(func.Focus, prefix + "-- ", context);
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
                    AppendResults($"{prefix}{func.ChildName} *invalid property name*", true);
                }
                return context;
            }
            if (expr is FunctionCallExpression)
            {
                var func = expr as FunctionCallExpression;
                var funcs = _compiler.Symbols.Filter(func.FunctionName, func.Arguments.Count() + 1);
                if (funcs.Count() == 0 && !(expr is BinaryExpression))
                {
                    AppendResults($"{prefix}{func.FunctionName} *invalid function name*", true);
                }
                else
                {
                    AppendResults($"{prefix}{func.FunctionName}");
                }
                var focusContext = CheckExpression(func.Focus, prefix + "-- ", context);

                if (func.FunctionName == "binary.as")
                {
                    if (func.Arguments.Count() != 2)
                    {
                        AppendResults($"{prefix}{func.FunctionName} INVALID AS Operation", true);
                        return focusContext;
                    }
                    var argContextResult = CheckExpression(func.Arguments.First(), prefix + "    ", focusContext);
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
                        var argContextResult = CheckExpression(op, prefix + "    ", focusContext);

                        // Filter the values that are not in this set
                        focusContext._2gs = argContextResult._2gs;
                        focusContext._2qs = argContextResult._2qs;
                    }
                    else
                    {
                        foreach (var item in func.Arguments)
                        {
                            var argContextResult = CheckExpression(item, prefix + "    ", focusContext);
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
                AppendResults($"{prefix}{func.Value.ToString()} (constant)");
                return null; // context doesn't propogate from this
            }
            else if (expr is VariableRefExpression)
            {
                var func = expr as VariableRefExpression;
                // sb.AppendFormat("{0}{1} (variable ref)\r\n", prefix, func.Name);
                return context;
            }
            AppendResults(expr.GetType().ToString());
            return context;
        }
    }
}
