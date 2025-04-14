namespace VEmu;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        ScreenBox = new PictureBox();
        ((System.ComponentModel.ISupportInitialize)ScreenBox).BeginInit();
        SuspendLayout();
        // 
        // ScreenBox
        // 
        ScreenBox.Anchor = AnchorStyles.None;
        ScreenBox.Location = new Point(83, 19);
        ScreenBox.Name = "ScreenBox";
        ScreenBox.Size = new Size(192, 128);
        ScreenBox.TabIndex = 0;
        ScreenBox.TabStop = false;
        ScreenBox.Paint += ScreenBox_Paint;
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(356, 322);
        Controls.Add(ScreenBox);
        Name = "Form1";
        Text = "Form1";
        Paint += Form1_Paint;
        ((System.ComponentModel.ISupportInitialize)ScreenBox).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private PictureBox ScreenBox;
}
