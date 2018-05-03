using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SorterControl.Log4NetAppender
{
    public class TextBoxAppender : AppenderSkeleton
    {
        private RichTextBox _textBox;
        public string FormName { get; set; }
        public string TextBoxName { get; set; }
        delegate void PrintHandler(RichTextBox tb, string text);
        public static bool ShowDebug = false;
        public static bool ShowInfo = true;

        protected override void Append(LoggingEvent loggingEvent)
        {
            if (_textBox == null)
            {
                if (String.IsNullOrEmpty(FormName) ||
                    String.IsNullOrEmpty(TextBoxName))
                    return;

                Form form = Application.OpenForms[FormName];
                if (form == null)
                    return;

                _textBox = form.Controls.Find(TextBoxName, true).FirstOrDefault() as RichTextBox;
                if (_textBox == null)
                    return;

                form.FormClosing += (s, e) => _textBox = null;
            }

            //Thread Td = new Thread(() => Print(_textBox, loggingEvent.RenderedMessage + Environment.NewLine));
            //Td.IsBackground = true;
            //Td.Start();

            switch (loggingEvent.Level.DisplayName)
            {
                case "DEBUG":
                    if (!ShowDebug)
                    {
                        return;
                    }
                    break;
                case "INFO":
                    if (!ShowInfo)
                    {
                        return;
                    }
                    break;
            }

            Print(_textBox, loggingEvent.RenderedMessage + Environment.NewLine);



        }

        public static void Print(RichTextBox tb, string text)
        {
            //判斷這個TextBox的物件是否在同一個執行緒上
            if (tb.InvokeRequired)
            {
                //當InvokeRequired為true時，表示在不同的執行緒上，所以進行委派的動作!!
                PrintHandler ph = new PrintHandler(Print);
                tb.BeginInvoke(ph, tb, text);
            }
            else
            {
                //表示在同一個執行緒上了，所以可以正常的呼叫到這個TextBox物件
                if (tb.Text.Length > 3000)
                {
                    tb.Text = tb.Text.Substring(tb.Text.Length - 2900);
                }
                tb.AppendText(text);
                tb.ScrollToCaret();
            }
        }
    }
}
