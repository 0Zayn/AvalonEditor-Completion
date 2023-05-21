using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private LuaAutoCompletion luaAutoCompletion;

        public MainWindow()
        {
            InitializeComponent();

            var assembly = typeof(MainWindow).Assembly;
            var resourceName = "WpfApp1.lua.xshd";
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                using (var reader = new System.Xml.XmlTextReader(stream))
                {
                    textEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                }
            }

            luaAutoCompletion = new LuaAutoCompletion();
            luaAutoCompletion.EnableAutoCompletion(textEditor);
        }

        public class LuaAutoCompletion
        {
            private static readonly string[] LuaKeywords = {
            "and", "break", "do", "else", "elseif", "end", "false", "for", "function",
            "goto", "if", "in", "local", "nil", "not", "or", "repeat", "return", "then",
            "true", "until", "while"
        };

            private static readonly string[] LuaSymbols = { "+", "-", "*", "/", "=", "==", "~=", "<", ">", "<=", ">=" };

            private CompletionWindow completionWindow;
            private TextEditor textEditor;

            public void EnableAutoCompletion(TextEditor textEditor)
            {
                this.textEditor = textEditor;
                this.textEditor.TextArea.TextEntered += TextEditor_TextEntered;
                this.textEditor.TextArea.KeyDown += TextEditor_KeyDown;
            }

            private void TextEditor_TextEntered(object sender, TextCompositionEventArgs e)
            {
                var word = GetWordBeforeCaret();

                if (word.EndsWith(e.Text))
                {
                    ShowCompletion();
                    FilterCompletionList(word);
                }
                else
                {
                    CloseCompletionWindow();
                }
            }

            private void CloseCompletionWindow()
            {
                completionWindow?.Close();
                completionWindow = null;
            }

            private string GetWordBeforeCaret()
            {
                var caretOffset = textEditor.TextArea.Caret.Offset;
                var document = textEditor.Document;
                var startOffset = caretOffset - 1;

                while (startOffset >= 0)
                {
                    var ch = document.GetCharAt(startOffset);
                    if (!char.IsLetterOrDigit(ch))
                    {
                        break;
                    }
                    startOffset--;
                }

                return document.GetText(startOffset + 1, caretOffset - startOffset - 1);
            }

            private void FilterCompletionList(string filter)
            {
                if (completionWindow != null && completionWindow.Content is CompletionList completionList)
                {
                    var filteredData = LuaKeywords.Concat(LuaSymbols)
                        .Where(item => item.StartsWith(filter, StringComparison.OrdinalIgnoreCase))
                        .Select(item => new LuaCompletionData(item));

                    completionList.CompletionData.Clear();
                    foreach (var item in filteredData)
                    {
                        completionList.CompletionData.Add(item);
                    }

                    completionWindow.Show();
                    completionWindow.CompletionList.SelectItem(filter);
                }
            }

            private void TextEditor_KeyDown(object sender, KeyEventArgs e)
            {
                if (completionWindow != null)
                {
                    if (e.Key == Key.Enter || e.Key == Key.Tab)
                    {
                        completionWindow.CompletionList.RequestInsertion(e);
                        e.Handled = true;
                    }
                    else if (e.Key == Key.Escape)
                    {
                        completionWindow.Close();
                        e.Handled = true;
                    }
                }
            }

            private void ShowCompletion()
            {
                if (completionWindow == null)
                {
                    var data = LuaKeywords.Concat(LuaSymbols);
                    var completionList = new CompletionList();

                    completionList.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#141414"));
                    completionList.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dedede"));

                    foreach (var item in data)
                    {
                        completionList.CompletionData.Add(new LuaCompletionData(item));
                    }

                    completionWindow = new CompletionWindow(textEditor.TextArea);
                    completionWindow.Content = completionList;
                    completionWindow.Show();
                    completionWindow.Closed += (sender, e) => completionWindow = null;

                    completionWindow.StartOffset = textEditor.TextArea.Caret.Offset;
                }
            }

            private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
            {
                if (completionWindow != null && completionWindow.CompletionList.SelectedItem is LuaCompletionData item)
                {
                    var segment = new TextSegment()
                    {
                        StartOffset = textEditor.TextArea.Caret.Offset - item.Text.Length,
                        Length = item.Text.Length
                    };
                    item.Complete(textEditor.TextArea, segment, EventArgs.Empty);
                }
            }
        }

        public class LuaCompletionData : ICompletionData
        {
            public LuaCompletionData(string text)
            {
                Text = text;
            }

            public ImageSource Image => null; // useless, I just need it for ICompletionData

            public string Text { get; }

            public object Content => Text; // useless, I just need it for ICompletionData

            public object Description => null; // useless, I just need it for ICompletionData

            public double Priority => 0; // useless, I just need it for ICompletionData

            public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
            {
                textArea.Document.Replace(completionSegment, Text);
            }
        }
    }
}
