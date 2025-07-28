using System;
using System.Drawing;
using System.Windows.Forms;

namespace ClassLibrary.Dialog;

public static class Dialog
{
    /// <summary>
    /// Creates a generic dialog with a message and a button.
    /// </summary>
    public static void CreateGenericDialog(string title, string message, string buttonText, IWin32Window owner)
    {
        // Create a temporary label to measure the text
        using (var tempLabel = new Label() { Text = message, AutoSize = true })
        using (var dialog = new Form()
        {
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            Text = title,
            ControlBox = false,
            ShowInTaskbar = false, // Hide from taskbar
            MinimumSize = new Size(250, 150), // Set minimum size to ensure dialog is not too small
            MaximumSize = new Size(800, 600)  // Set maximum size to prevent dialog from being too wide
        })
        {
            // Calculate required width based on message text
            int textWidth = TextRenderer.MeasureText(tempLabel.Text, tempLabel.Font).Width;

            // Add padding (40px for left + right margins)
            int requiredWidth = Math.Min(Math.Max(textWidth + 80, 250), 800);

            // Set dialog dimensions
            dialog.Width = requiredWidth;
            dialog.Height = 150;

            var messageLabel = new Label()
            {
                Text = message,
                Left = 20,
                Top = 20,
                AutoSize = true,
                MaximumSize = new Size(dialog.Width - 40, 0) // Ensure text wraps if too long
            };

            var button = new Button()
            {
                Text = buttonText,
                Width = 100,
                Height = 40,
                Top = 60,
                Left = (dialog.Width - 100) / 2, // Center the button
                DialogResult = DialogResult.OK
            };

            dialog.Controls.Add(messageLabel);
            dialog.Controls.Add(button);

            // Adjust height based on actual text height
            int textHeight = TextRenderer.MeasureText(messageLabel.Text, messageLabel.Font,
                new Size(messageLabel.MaximumSize.Width, int.MaxValue),
                TextFormatFlags.WordBreak).Height;

            if (textHeight > messageLabel.Height)
            {
                dialog.Height += (textHeight - messageLabel.Height) + 20; // Add some extra padding
            }

            dialog.AcceptButton = button;
            dialog.ShowDialog(owner);
        }
    }

    /// <summary>
    /// Creates a quit dialog with a message and a button to log in again.
    /// </summary>
    public static bool CreateQuitDialog(string title, string message, string buttonText, IWin32Window owner)
    {
        // Create a custom dialog
        using (var tempLabel = new Label() { Text = message, AutoSize = true })
        using (var quitDialog = new Form()
        {
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            Text = title,
            ControlBox = true,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false, // Hide from taskbar
            MinimumSize = new Size(250, 150), // Set minimum size to ensure dialog is not too small
            MaximumSize = new Size(800, 600)  // Set maximum size to prevent dialog from being too wide
        })
        {
            // Calculate required width based on message text
            int textWidth = TextRenderer.MeasureText(tempLabel.Text, tempLabel.Font).Width;

            // Add padding (40px for left + right margins)
            int requiredWidth = Math.Min(Math.Max(textWidth + 80, 250), 800);

            // Set dialog dimensions
            quitDialog.Width = requiredWidth;
            quitDialog.Height = 160;

            var messageLabel = new Label()
            {
                Text = message,
                Left = 20,
                Top = 20,
                AutoSize = true,
                MaximumSize = new Size(quitDialog.Width - 40, 0) // Ensure text wraps if too long
            };

            var loginButton = new Button()
            {
                Text = buttonText,
                Left = (quitDialog.Width - 100) / 2, // Center the button
                Width = 100,
                Height = 40,
                Top = 70,
                DialogResult = DialogResult.OK
            };

            loginButton.Click += (s, e) => { quitDialog.DialogResult = DialogResult.OK; };
            quitDialog.FormClosing += (s, e) =>
            {
                // If user clicks the X button
                if (quitDialog.DialogResult != DialogResult.OK)
                {
                    Application.Exit();
                }
            };

            quitDialog.AcceptButton = loginButton; // Allow pressing Enter to click the button
            quitDialog.Controls.Add(messageLabel);
            quitDialog.Controls.Add(loginButton);

            // Adjust height based on actual text height
            int textHeight = TextRenderer.MeasureText(messageLabel.Text, messageLabel.Font,
                new Size(messageLabel.MaximumSize.Width, int.MaxValue),
                TextFormatFlags.WordBreak).Height;

            if (textHeight > messageLabel.Height)
            {
                quitDialog.Height += (textHeight - messageLabel.Height) + 20; // Add some extra padding
            }

            // Show the dialog
            var result = quitDialog.ShowDialog(owner);

            // Reset the game state if Login Again was clicked
            if (result == DialogResult.OK)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}