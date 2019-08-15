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

using fp3 = stu3.Hl7.Fhir.FhirPath;
using f3 = stu3.Hl7.Fhir.Model;

using fp4 = r4.Hl7.Fhir.FhirPath;
using f4 = r4.Hl7.Fhir.Model;

using Hl7.FhirPath;
using Hl7.FhirPath.Expressions;
using System.Text;
using Windows.UI.Xaml.Documents;
using FhirPathTester;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            // hamburgerMenuControl.MenuItemsSource = items;
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

        private ITypedElement GetResourceNavigator(out EvaluationContext evalContext)
        {
            string parseErrors2;
            var inputNavDSTU2 = FhirPathProcessor.GetResourceNavigatorDSTU2(textboxInputXML.Text, out parseErrors2);
            string parseErrors3;
            var inputNavSTU3 = FhirPathProcessor.GetResourceNavigatorSTU3(textboxInputXML.Text, out parseErrors3);
            string parseErrors4;
            var inputNavR4 = FhirPathProcessor.GetResourceNavigatorR4(textboxInputXML.Text, out parseErrors4);

            if (!string.IsNullOrEmpty(parseErrors2) || !string.IsNullOrEmpty(parseErrors3) || !string.IsNullOrEmpty(parseErrors4))
            {
                ResetResults();
                AppendResults(String.Join("\r\n--------------------\r\n", parseErrors2, parseErrors3, parseErrors4), true);
            }

            if (inputNavR4 != null)
                btnR4.Visibility = Visibility.Visible;
            else
                btnR4.Visibility = Visibility.Collapsed;

            if (inputNavSTU3 != null)
                btnSTU3.Visibility = Visibility.Visible;
            else
                btnSTU3.Visibility = Visibility.Collapsed;

            if (inputNavDSTU2 != null)
                btnDSTU2.Visibility = Visibility.Visible;
            else
                btnDSTU2.Visibility = Visibility.Collapsed;

            if (inputNavSTU3 != null)
            {
                evalContext = new fp3.FhirEvaluationContext(inputNavSTU3);
                return inputNavSTU3;
            }
            if (inputNavDSTU2 != null)
            {
                evalContext = new fp2.FhirEvaluationContext(inputNavDSTU2);
                return inputNavDSTU2;
            }
            evalContext = new fp4.FhirEvaluationContext(inputNavR4);
            return inputNavR4;
        }

        private void ButtonGo_Click(object sender, RoutedEventArgs e)
        {
            EvaluationContext evalContext;
            var inputNav = GetResourceNavigator(out evalContext);
            if (inputNav == null)
                return;

            // Don't need to cache this, it is cached in the fhir-client
            CompiledExpression xps = null;
            try
            {
                xps = FhirPathProcessor._compiler.Compile(textboxExpression.Text);
            }
            catch (Exception ex)
            {
                SetResults("Expression compilation error:\r\n" + ex.Message, true);
                return;
            }

            IEnumerable<ITypedElement> prepopulatedValues = null;
            if (xps != null)
            {
                try
                {
                    prepopulatedValues = xps(inputNav, evalContext);
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
                        foreach (var item in prepopulatedValues)
                        {
                            string tooltip = item.Annotation<IShortPathGenerator>()?.ShortPath;
                            if (item is stu3.Hl7.Fhir.ElementModel.IFhirValueProvider)
                            {
                                foreach (var t2 in fp3.ElementNavFhirExtensions.ToFhirValues(new ITypedElement[] { item }).Where(i => i != null))
                                {
                                    // output the content as XML fragments
                                    var fragment = stu3.Hl7.Fhir.Serialization.FhirSerializer.SerializeToXml(t2, root: t2.TypeName);
                                    fragment = AppendXmlFramentResults(fragment, tooltip);
                                }
                            }
                            else if (item is dstu2.Hl7.Fhir.ElementModel.IFhirValueProvider)
                            {
                                foreach (var t2 in fp2.ElementNavFhirExtensions.ToFhirValues(new ITypedElement[] { item }).Where(i => i != null))
                                {
                                    // output the content as XML fragments
                                    var fragment = dstu2.Hl7.Fhir.Serialization.FhirSerializer.SerializeToXml(t2, root: t2.TypeName);
                                    fragment = AppendXmlFramentResults(fragment, tooltip);
                                }
                            }
                            else if (item is r4.Hl7.Fhir.ElementModel.IFhirValueProvider)
                            {
                                foreach (var t2 in fp4.ElementNavFhirExtensions.ToFhirValues(new ITypedElement[] { item }).Where(i => i != null))
                                {
                                    // output the content as XML fragments
                                    var fragment = r4.Hl7.Fhir.Serialization.FhirSerializer.SerializeToXml(t2, root: t2.TypeName);
                                    fragment = AppendXmlFramentResults(fragment, tooltip);
                                }
                            }
                            else if (item is ITypedElement te)
                            {
                                AppendResults($"<{te.InstanceType} value=\"{te.Value}\">");
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

        private string AppendXmlFramentResults(string fragment, string tooltip)
        {
            if (fragment.Length > 100)
            {
                // pretty print the content
                var doc = System.Xml.Linq.XDocument.Parse(fragment);
                fragment = doc.ToString(System.Xml.Linq.SaveOptions.None);
            }
            AppendResults(fragment.Replace(" xmlns=\"http://hl7.org/fhir\"", ""), false, tooltip);
            return fragment;
        }

        private void AppendParseTree()
        {
            // Grab the parse expression
            StringBuilder sb = new StringBuilder();
            var expr = FhirPathProcessor._compiler.Parse(textboxExpression.Text);
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

        private void ButtonPredicate_Click(object sender, RoutedEventArgs e)
        {
            EvaluationContext evalContext;
            var inputNav = GetResourceNavigator(out evalContext);
            if (inputNav == null)
                return;

            // Don't need to cache this, it is cached in the fhir-client
            Hl7.FhirPath.CompiledExpression xps = null;
            try
            {
                xps = FhirPathProcessor._compiler.Compile(textboxExpression.Text);
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
                    bool result = xps.Predicate(inputNav, evalContext);
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
            try
            {
                // This is the place where we want to support the reading of the file from the file system
                // to make the testing of other instances really easy
                if (e.DataView != null)
                {
                    var formats = e.DataView.AvailableFormats;

                    if (formats.Contains(StandardDataFormats.WebLink))
                    {
                        Uri webLink = await e.DataView.GetWebLinkAsync();
                        if (!string.IsNullOrEmpty(webLink.OriginalString))
                        {
                            HttpClient client = new HttpClient();
                            string contents = await client.GetStringAsync(webLink);
                            textboxInputXML.Text = contents;
                        }
                        e.Handled = true;
                        return;
                    }

                    if (formats.Contains(StandardDataFormats.StorageItems))
                    {
                        var items = await e.DataView.GetStorageItemsAsync();
                        if (items.Count > 0)
                        {
                            if (items[0].IsOfType(StorageItemTypes.File))
                            {
                                string contents = await FileIO.ReadTextAsync((StorageFile)items[0]);
                                textboxInputXML.Text = contents;
                            }
                        }
                        e.Handled = true;
                    }
                }
            }
            catch (Exception ex)
            {

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
            EvaluationContext evalContext;
            var inputNav = GetResourceNavigator(out evalContext);
            if (inputNav == null)
                return;

            // Don't need to cache this, it is cached in the fhir-client
            Hl7.FhirPath.Expressions.Expression expr = null;
            try
            {
                expr = FhirPathProcessor._compiler.Parse(textboxExpression.Text);
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
                    FhirPathProcessor.CheckExpression(inputNav, expr, AppendResults, ResetResults);
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
                // try this one out sometime
                // https://stackoverflow.com/questions/27649534/set-tooltip-on-range-of-text-in-wpf-richtextbox
                ToolTip tip = new ToolTip();
                tip.Content = tooltip;
                ToolTipService.SetToolTip(para, tip);

                //tooltopTemp.Content = tooltip;
                //ToolTipService.SetToolTip(para, tooltopTemp);

                var run2 = new Run() { Text = " " + tooltip.Replace("\r\n", ", ") };
                run2.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Gray);
                run2.FontStyle = Windows.UI.Text.FontStyle.Italic;
                para.Inlines.Add(run2);
            }
        }

        private void BtnXML_Click(object sender, RoutedEventArgs e)
        {
            FhirPathProcessor.PretifyXML(textboxInputXML.Text, (val) => { textboxInputXML.Text = val; });
        }

        private void BtnJson_Click(object sender, RoutedEventArgs e)
        {
            FhirPathProcessor.PretifyJson(textboxInputXML.Text, (val) => { textboxInputXML.Text = val; });
        }
    }
}
