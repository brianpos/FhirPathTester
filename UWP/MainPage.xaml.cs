﻿extern alias dstu2;
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
using System.Collections.ObjectModel;
using Windows.ApplicationModel;
using Hl7.Fhir.Utility;

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

            // and remember the initial state
            AddHistoryEntry(textboxInputXML.Text, textboxExpression.Text);
            System.Threading.Tasks.Task.Run(() => {
                // in the background load up the 3 sets of ClassLibraries
                r4.Hl7.Fhir.Serialization.BaseFhirParser.Inspector.Import(typeof(r4.Hl7.Fhir.Serialization.BaseFhirParser).Assembly);
                // stu3.Hl7.Fhir.Serialization.BaseFhirParser.Inspector.Import(typeof(stu3.Hl7.Fhir.Serialization.BaseFhirParser).Assembly);
                dstu2.Hl7.Fhir.Serialization.BaseFhirParser.Inspector.Import(typeof(dstu2.Hl7.Fhir.Serialization.BaseFhirParser).Assembly);
                FhirPathProcessor.GetResourceNavigatorSTU3("<Patient xmlns=\"http://hl7.org/fhir\"/>", out var errs);
            });
        }

        public ObservableCollection<HistoryItemDetails> HistoryItems { get; set; }
            = new ObservableCollection<HistoryItemDetails>();
        //{
        //    new Tuple<string, string, string>($"{DateTime.Now.ToString()}", "<Patient xmlns=\"http://hl7.org/fhir\">\r\n  <name>\r\n    <given value=\"brian\"/>\r\n  </name>\r\n  <birthDate value=\"1980\"/>\r\n</Patient>", ""),
        //    new Tuple<string, string, string>($"{DateTime.Now.ToString()}", "<Patient xmlns=\"http://hl7.org/fhir\">\r\n  <name>\r\n    <given value=\"allen\"/>\r\n  </name>\r\n  <birthDate value=\"1964\"/>\r\n</Patient>", "")
        //};
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

        private System.Collections.Generic.SortedList<int, string> _locations = new SortedList<int, string>();
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
            if (inputNavR4 != null || inputNavSTU3 != null || inputNavDSTU2 != null)
            {
                ISourceNode node;
                if (textboxInputXML.Text.StartsWith("{"))
                    node = Hl7.Fhir.Serialization.FhirJsonNode.Parse(textboxInputXML.Text);
                else
                    node = Hl7.Fhir.Serialization.FhirXmlNode.Parse(textboxInputXML.Text);
                _locations.Clear();
                int lastPos = 0;
                IPositionInfo lastNode = null;
                AddLocations(node, ref lastNode, ref lastPos, textboxInputXML.Text.ToCharArray());
                string t = _locations.LastOrDefault(c => c.Key < textboxInputXML.SelectionStart).Value;
                System.Diagnostics.Trace.WriteLine($"Focused: {t}");
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

            if (inputNavR4 != null)
            {
                evalContext = new fp4.FhirEvaluationContext(inputNavR4);
                return inputNavR4;
            }
            if (inputNavSTU3 != null)
            {
                evalContext = new fp3.FhirEvaluationContext(inputNavSTU3);
                return inputNavSTU3;
            }
            evalContext = new fp2.FhirEvaluationContext(inputNavDSTU2);
            return inputNavDSTU2;
        }

        private void AddLocations(ISourceNode node, ref IPositionInfo lastNode, ref int lastCharPos, char[] chars)
        {
            int location = 0;
            IPositionInfo posInfo = (node as IAnnotated)?.Annotation<Hl7.Fhir.Serialization.XmlSerializationDetails>();
            if (posInfo == null)
                posInfo = (node as IAnnotated)?.Annotation<Hl7.Fhir.Serialization.JsonSerializationDetails>();
            if (posInfo != null)
            {
                if (lastNode == null)
                    location = 0;
                else
                {
                    var linesToSkip = posInfo.LineNumber - lastNode.LineNumber;
                    var colsToSkip = posInfo.LinePosition;
                    if (linesToSkip == 0)
                        colsToSkip -= lastNode.LinePosition;
                    while (linesToSkip > 0 && lastCharPos < chars.Length)
                    {
                        lastCharPos++;
                        if (chars[lastCharPos] == '\r')
                            linesToSkip--;
                    }
                    lastCharPos += colsToSkip;
                    location = lastCharPos; // need to patch this
                }
                lastNode = posInfo;
                _locations.Add(location, node.Location);
                // System.Diagnostics.Trace.WriteLine($"{location}: {node.Location}");
            }
            lastCharPos = location;
            foreach (var child in node.Children())
            {
                AddLocations(child, ref lastNode, ref lastCharPos, chars);
            }
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
                AddHistoryEntry(textboxInputXML.Text, textboxExpression.Text);

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

        private void AddHistoryEntry(string content, string expression)
        {
            if (HistoryItems.Count > 0)
            {
                // check to see if the content has changed
                var lastEntry = HistoryItems[0];
                if (lastEntry.ResourceContent == content && lastEntry.Expression == expression)
                    return; // (no need to add a new entry)
            }
            HistoryItems.Insert(0, new HistoryItemDetails(content, expression));
            // trim the history to only 100 items
            while (HistoryItems.Count > 100)
                HistoryItems.Remove(HistoryItems.Last());
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
                    AddHistoryEntry(textboxInputXML.Text, textboxExpression.Text);
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
                            using (HttpClient client = new HttpClient())
                            {
                                using (var response = await client.GetAsync(webLink))
                                {
                                    string contents = await response.Content.ReadAsStringAsync();
                                    textboxInputXML.Text = contents;
                                    if (response.Content.Headers.ContentType.MediaType.Contains("xml"))
                                        FhirPathProcessor.PretifyXML(textboxInputXML.Text, (val) => { textboxInputXML.Text = val; });
                                    if (response.Content.Headers.ContentType.MediaType.Contains("json"))
                                        FhirPathProcessor.PretifyJson(textboxInputXML.Text, (val) => { textboxInputXML.Text = val; });
                                    AddHistoryEntry(textboxInputXML.Text, textboxExpression.Text);
                                }
                            }
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
                                AddHistoryEntry(textboxInputXML.Text, textboxExpression.Text);
                            }
                        }
                        e.Handled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
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
            _tooltipText.Clear();
            ToolTipService.SetToolTip(textboxResult, null);
        }

        private void SetResults(string text, bool error = false)
        {
            ResetResults();
            var run = new Run() { Text = text };
            var para = new Paragraph();
            para.Inlines.Add(run);
            textboxResult.Blocks.Add(para);
            if (error)
            {
                para.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Red);
                para.FontWeight = Windows.UI.Text.FontWeights.Bold;
            }
        }

        Dictionary<Run, string> _tooltipText = new Dictionary<Run, string>();
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
            if (!string.IsNullOrEmpty(tooltip))
            {
                // try this one out sometime
                // https://stackoverflow.com/questions/27649534/set-tooltip-on-range-of-text-in-wpf-richtextbox
                _tooltipText.Add(run, tooltip);
                if (ToolTipService.GetToolTip(textboxResult) == null)
                {
                    ToolTip tip = new ToolTip();
                    tip.Content = tooltip;
                    ToolTipService.SetToolTip(textboxResult, tip);
                    tip.Opened += Tip_Opened;
                    tip.Closed += Tip_Closed;
                }

                //var run2 = new Run() { Text = " " + tooltip.Replace("\r\n", ", ") };
                //run2.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Gray);
                //run2.FontStyle = Windows.UI.Text.FontStyle.Italic;
                //para.Inlines.Add(run2);
            }
        }

        private void Tip_Closed(object sender, RoutedEventArgs e)
        {
        }

        private void Tip_Opened(object sender, RoutedEventArgs e)
        {
            // current mouse position
            var pointerPosition = Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerPosition;
            var x = pointerPosition.X - Window.Current.Bounds.X;
            var y = pointerPosition.Y - Window.Current.Bounds.Y;
            ToolTip tip = sender as ToolTip;
            var ttv = textboxResult.TransformToVisual(Window.Current.Content);
            var sc = ttv.TransformPoint(new Windows.Foundation.Point(0, 0));
            var tp = textboxResult.GetPositionFromPoint(new Windows.Foundation.Point(x - sc.X, y - sc.Y));
            if (tp.Parent is Run run)
            {
                if (_tooltipText.ContainsKey(run))
                {
                    tip.Content = _tooltipText[run];
                }
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

        private void TextboxInputXML_SelectionChanged(object sender, RoutedEventArgs e)
        {
            CursorPosition(textboxInputXML, out int row, out int col);
            textPosition.Text = $"Ln {row} Col {col}";
            if (_locations.Count > 0)
            {
                string t = _locations.LastOrDefault(c => c.Key < textboxInputXML.SelectionStart).Value;
                labelStatus.Text = $"{t}";
            }
        }

        /// <summary>
        /// Returns the current column position on the current line the cursor is on.
        /// </summary>
        public static void CursorPosition(TextBox tb, out int row, out int col)
        {
            int endMarker = tb.SelectionStart;

            if (endMarker == 0)
            {
                row = 1;
                col = 1;
                return;
            }

            int i = 0;
            col = 1;
            row = 1;

            foreach (char c in tb.Text)
            {
                i++;
                col++;

                if (c == '\r')
                {
                    row++;
                    col = 1;
                }

                if (i == endMarker)
                {
                    return;
                }
            }
        }

        private void ListHistory_ItemClick(object sender, ItemClickEventArgs e)
        {
            // read the value from the list item
            var item = e.ClickedItem as HistoryItemDetails;
            if (!string.IsNullOrEmpty(item.ResourceContent))
                textboxInputXML.Text = item.ResourceContent;
            if (!string.IsNullOrEmpty(item.Expression))
                textboxExpression.Text = item.Expression;
        }

        private void HamburgerMenuControl_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            var item = args.InvokedItem as string;
            if (item == "History")
                listHistory.Visibility = Visibility.Visible;
            else
                listHistory.Visibility = Visibility.Collapsed;
            if (item == "About")
                markdownHost.Visibility = Visibility.Visible;
            else
                markdownHost.Visibility = Visibility.Collapsed;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            string initalMarkdownText = await FileIO.ReadTextAsync(await Package.Current.InstalledLocation.GetFileAsync("about.md"));
            markdownAboutBox.Text = initalMarkdownText;
        }

        private void TextboxResult_PointerMoved(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            ToolTip temp = ToolTipService.GetToolTip(textboxResult) as ToolTip;
            if (temp != null)
            {
                // current mouse position
                //var pointerPosition = Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerPosition;
                //var x = pointerPosition.X - Window.Current.Bounds.X;
                //var y = pointerPosition.Y - Window.Current.Bounds.Y;
                //var ttv = textboxResult.TransformToVisual(Window.Current.Content);
                //var sc = ttv.TransformPoint(new Windows.Foundation.Point(0, 0));
                var tp = textboxResult.GetPositionFromPoint(e.GetCurrentPoint(textboxResult).Position);
                if (tp.Parent is Run run)
                {
                    if (_tooltipText.ContainsKey(run))
                    {
                        temp.Content = _tooltipText[run];
                        if (!temp.IsOpen)
                        {
                            temp.IsOpen = true;
                        }
                        e.Handled = true;
                    }
                }
            }
        }

        private void TextboxResult_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            ToolTip temp = ToolTipService.GetToolTip(textboxResult) as ToolTip;
            if (temp?.IsOpen == true)
                temp.IsOpen = false;
        }
    }
}
