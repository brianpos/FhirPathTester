extern alias r5;
extern alias stu3;
extern alias r4;

using Hl7.Fhir.ElementModel;

using fp2 = r5.Hl7.Fhir.FhirPath;
using f2 = r5.Hl7.Fhir.Model;

using fp3 = stu3.Hl7.Fhir.FhirPath;
using f3 = stu3.Hl7.Fhir.Model;

using fp4 = r4.Hl7.Fhir.FhirPath;
using f4 = r4.Hl7.Fhir.Model;

using Hl7.FhirPath;
using Hl7.FhirPath.Expressions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Media;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Hl7.Fhir.FhirPath;

namespace FhirPathTester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public int TextControlFontSize { get; set; }

        public MainWindow()
        {
            TextControlFontSize = 22;
            InitializeComponent();
            textboxInputXML.Text = "<Patient xmlns=\"http://hl7.org/fhir\">\r\n<name>\r\n</name>\r\n<birthDate value=\"1980\"/>\r\n</Patient>";
            textboxExpression.Text = "birthDate < today() | identifier.assigner.display.extension.where(url='3').value.code";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private ITypedElement GetResourceNavigator(out EvaluationContext evalContext)
        {
            string parseErrors2;
            var inputNavDSTU2 = FhirPathProcessor.GetResourceNavigatorR5(textboxInputXML.Text, out parseErrors2);
            string parseErrors3;
            var inputNavSTU3 = FhirPathProcessor.GetResourceNavigatorSTU3(textboxInputXML.Text, out parseErrors3);
            string parseErrors4;
            var inputNavR4 = FhirPathProcessor.GetResourceNavigatorR4(textboxInputXML.Text, out parseErrors4);

            if (!string.IsNullOrEmpty(parseErrors2) || !string.IsNullOrEmpty(parseErrors3) || !string.IsNullOrEmpty(parseErrors4))
            {
                ResetResults();
                textboxResult.AppendText(String.Join("\r\n--------------------\r\n", parseErrors2, parseErrors3, parseErrors4));
            }

            if (inputNavR4 != null)
                labelR4.Visibility = Visibility.Visible;
            else
                labelR4.Visibility = Visibility.Collapsed;
            if (inputNavSTU3 != null)
                labelSTU3.Visibility = Visibility.Visible;
            else
                labelSTU3.Visibility = Visibility.Collapsed;

            if (inputNavDSTU2 != null)
                labelDSTU2.Visibility = Visibility.Visible;
            else
                labelDSTU2.Visibility = Visibility.Collapsed;

            _traceData.Clear();
            if (inputNavR4 != null)
            {
                evalContext = new FhirEvaluationContext(inputNavR4);
                evalContext.Tracer += TraceExcutionCall;
                return inputNavR4;
            }
            if (inputNavSTU3 != null)
            {
                evalContext = new FhirEvaluationContext(inputNavSTU3);
                evalContext.Tracer += TraceExcutionCall;
                return inputNavSTU3;
            }
            evalContext = new FhirEvaluationContext(inputNavDSTU2);
            evalContext.Tracer += TraceExcutionCall;
            return inputNavDSTU2;
        }

        private void TraceExcutionCall(string key, IEnumerable<ITypedElement> values)
        {
            _traceData.Add(new KeyValuePair<string, IEnumerable<ITypedElement>>(key, values));
            System.Diagnostics.Trace.WriteLine($"key: ???");
        }
        private List<KeyValuePair<string, IEnumerable<ITypedElement>>> _traceData = new List<KeyValuePair<string, IEnumerable<ITypedElement>>>();

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
                SetResults("Expression compilation error:\r\n" + ex.Message);
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
                    FhirPathProcessor.ProcessPrepopulatedValues(prepopulatedValues, AppendXmlFramentResults, AppendResults);
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

        private void textboxExpression_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 100 || e.Delta < -100)
            {
                TextControlFontSize += e.Delta / 100;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TextControlFontSize)));
            }
        }
        #region << Drag Drop Functionality >>
        private void textboxInputXML_Drop(object sender, DragEventArgs e)
        {
            // This is the place where we want to support the reading of the file from the file system
            // to make the testing of other instances really easy
            var formats = e.Data.GetFormats();
            if (e.Data.GetDataPresent("FileName"))
            {
                string[] contents = e.Data.GetData("FileName") as string[];
                if (contents.Length > 0)
                    textboxInputXML.Text = System.IO.File.ReadAllText(contents[0]);
                e.Handled = true;
            }
        }

        private void textboxInputXML_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy | DragDropEffects.Link;
        }

        private void textboxInputXML_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }
        #endregion

        private void textboxInputXML_SelectionChanged(object sender, RoutedEventArgs e)
        {
            int lineIndex = 0;
            int colIndex = 0;
            try
            {
                int charIndex = textboxInputXML.CaretIndex;
                lineIndex = textboxInputXML.GetLineIndexFromCharacterIndex(charIndex);
                colIndex = charIndex - textboxInputXML.GetCharacterIndexFromLineIndex(lineIndex);
            }
            catch (Exception)
            {
            }
            textboxRow.Content = $"Ln {lineIndex + 1}";
            textboxCol.Content = $"Col {colIndex}";
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
            textboxResult.Document.Blocks.Clear();
        }

        private void SetResults(string text, bool error = false)
        {
            textboxResult.Document.Blocks.Clear();
            var run = new Run(text);
            var para = new Paragraph(run);
            textboxResult.Document.Blocks.Add(para);
            if (error)
            {
                para.Foreground = Brushes.Red;
                para.FontWeight = FontWeights.Bold;
            }
        }
        private void AppendResults(string text, bool error = false, string tooltip = null)
        {
            var run = new Run(text);
            var para = new Paragraph(run);
            if (!string.IsNullOrEmpty(tooltip))
                para.ToolTip = tooltip;
            para.Margin = new Thickness(2);
            textboxResult.Document.Blocks.Add(para);
            if (error)
            {
                para.Foreground = Brushes.Red;
                para.FontWeight = FontWeights.Bold;
            }
        }

        private void lblXML_MouseUp(object sender, MouseButtonEventArgs e)
        {
            FhirPathProcessor.PretifyXML(textboxInputXML.Text, (val) => { textboxInputXML.Text = val; });
        }

        private void lblJson_MouseUp(object sender, MouseButtonEventArgs e)
        {
            FhirPathProcessor.PretifyJson(textboxInputXML.Text, (val) => { textboxInputXML.Text = val; });
        }
    }
}
