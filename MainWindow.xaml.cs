extern alias dstu2;
extern alias stu3;

using Hl7.ElementModel;

using fp2 = dstu2.Hl7.Fhir.FhirPath;
using f2 = dstu2.Hl7.Fhir.Model;

using fp3 = stu3.Hl7.Fhir.FhirPath;
using f3 = stu3.Hl7.Fhir.Model;

using Hl7.FhirPath;
using Hl7.FhirPath.Expressions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

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
            textboxInputXML.Text = "<Patient xmlns=\"http://hl7.org/fhir\">\r\n<name>\r\n</name>\r\n<birthDate value=\"1973\"/>\r\n</Patient>";
            textboxExpression.Text = "birthDate < today()";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private IElementNavigator GetResourceNavigatorDSTU2(out string parseErrors)
        {
            f2.Resource resource = null;
            try
            {
                if (textboxInputXML.Text.StartsWith("{"))
                    resource = new dstu2.Hl7.Fhir.Serialization.FhirJsonParser().Parse<f2.Resource>(textboxInputXML.Text);
                else
                    resource = new dstu2.Hl7.Fhir.Serialization.FhirXmlParser().Parse<f2.Resource>(textboxInputXML.Text);
            }
            catch (Exception ex)
            {
                parseErrors = "DSTU2 Resource read error:\r\n" + ex.Message;
                return null;
            }
            parseErrors = null;
            var inputNav = new fp2.PocoNavigator(resource);
            return inputNav;
        }
        private IElementNavigator GetResourceNavigatorSTU3(out string parseErrors)
        {
            f3.Resource resource = null;
            try
            {
                if (textboxInputXML.Text.StartsWith("{"))
                    resource = new stu3.Hl7.Fhir.Serialization.FhirJsonParser().Parse<f3.Resource>(textboxInputXML.Text);
                else
                    resource = new stu3.Hl7.Fhir.Serialization.FhirXmlParser().Parse<f3.Resource>(textboxInputXML.Text);
            }
            catch (Exception ex)
            {
                parseErrors = "STU3 Resource read error:\r\n" + ex.Message;
                return null;
            }
            parseErrors = null;
            var inputNav = new fp3.PocoNavigator(resource);
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
                textboxResult.Text = null;
                if (!string.IsNullOrEmpty(parseErrors2))
                    textboxResult.Text += parseErrors2;
                if (!string.IsNullOrEmpty(parseErrors2) && !string.IsNullOrEmpty(parseErrors3))
                    textboxResult.Text += "\r\n--------------------\r\n";
                if (!string.IsNullOrEmpty(parseErrors3))
                    textboxResult.Text += parseErrors3;
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
                textboxResult.Text = "Expression compilation error:\r\n" + ex.Message;
                return;
            }

            IEnumerable<IElementNavigator> prepopulatedValues = null;
            if (xps != null)
            {
                try
                {
                    prepopulatedValues = xps(inputNav, inputNav);
                }
                catch (Exception ex)
                {
                    textboxResult.Text = "Expression evaluation error:\r\n" + ex.Message;
                    AppendParseTree();
                    return;
                }

                textboxResult.Text = null;
                try
                {
                    if (prepopulatedValues.Count() > 0)
                    {
                        if (inputNav is fp3.PocoNavigator)
                        {
                            foreach (var t2 in prepopulatedValues.ToFhirValuesSTU3())
                            {
                                if (t2 != null)
                                {
                                    // output the content as XML fragments
                                    var fragment = stu3.Hl7.Fhir.Serialization.FhirSerializer.SerializeToXml(t2, root: t2.TypeName);
                                    textboxResult.Text += fragment.Replace(" xmlns=\"http://hl7.org/fhir\"", "") + "\r\n";
                                }
                                // System.Diagnostics.Trace.WriteLine(string.Format("{0}: {1}", xpath.Value, t2.AsStringRepresentation()));
                            }
                        }
                        if (inputNav is fp2.PocoNavigator)
                        {

                            foreach (var t2 in prepopulatedValues.ToFhirValuesDSTU2())
                            {
                                if (t2 != null)
                                {
                                    // output the content as XML fragments
                                    var fragment = dstu2.Hl7.Fhir.Serialization.FhirSerializer.SerializeToXml(t2, root: t2.TypeName);
                                    textboxResult.Text += fragment.Replace(" xmlns=\"http://hl7.org/fhir\"", "") + "\r\n";
                                }
                                // System.Diagnostics.Trace.WriteLine(string.Format("{0}: {1}", xpath.Value, t2.AsStringRepresentation()));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    textboxResult.Text = "Processing results error:\r\n" + ex.Message;
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
            textboxResult.Text += "\r\n\r\n----------------\r\n" + sb.ToString();
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
                textboxResult.Text = "Expression compilation error:\r\n" + ex.Message;
                return;
            }

            if (xps != null)
            {
                try
                {
                    var result = xps.Predicate(inputNav, inputNav);
                    textboxResult.Text = result.ToString();
                }
                catch (Exception ex)
                {
                    textboxResult.Text = "Expression evaluation error:\r\n" + ex.Message;
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
    }
}
