using System;
using System.ComponentModel;
using Avalonia.Controls;

namespace TrrntZipUIAvalonia
{
    public partial class ErrorLogWindow : Window
    {
        public bool closing = false;

        public ErrorLogWindow()
        {
            InitializeComponent();
            Closing += ErrorLogWindow_Closing;
        }

        private void ErrorLogWindow_Closing(object sender, CancelEventArgs e)
        {
            if (closing)
                return;

            this.Hide();
            e.Cancel = true;
        }

        public void AddError(string message)
        {
            Show();
            txtLog.Text = (txtLog.Text ?? "") + $"----{DateTime.Now}----\n{message}\n\n";

            // Scroll to end
            txtLog.CaretIndex = txtLog.Text?.Length ?? 0;
        }
    }
}
