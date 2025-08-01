using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ClassLibrary.Dialog;
using ClassLibrary.GlobalVariables;
using ClassLibrary.TextureManager;
using ClassLibrary.Wins;

namespace ClientApp;

public partial class MainMenu : Form
{
    private readonly Button btnStart;
    private readonly Button btnControls;
    private readonly Button btnChangeSkin;
    private readonly Button btnChangeMap;
    private readonly Button btnPreviewMap;
    private readonly Button btnQuit;
    private readonly FlowLayoutPanel buttonPanel;

    private int map = 1;
    private int skin = 1;

    private readonly Image? backgroundImage;

    private readonly Image? defaultButtonImage;
    private readonly Image? hoverButtonImage;

    public MainMenu()
    {
        //basic setup
        this.Text = "MoleThief - Main Menu";
        this.WindowState = FormWindowState.Maximized;
        this.DoubleBuffered = true;
        this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        this.UpdateStyles();
        this.MaximizeBox = true;

        string iconPath = Path.Combine("Textures", "icon.ico");
        if (File.Exists(iconPath))
        {
            this.Icon = new Icon(iconPath);
        }

        // Load custom background if present
        string bgPath = Path.Combine("Textures", GV.CurrentSkin.ToString(), "main_menu.png");
        if (File.Exists(bgPath))
        {
            backgroundImage = Image.FromFile(bgPath);
        }

        this.BackColor = Color.Black; // fallback when background missing / for letter-boxing

        //preload button textures
        if (File.Exists("Textures/button.png"))
            defaultButtonImage = Image.FromFile("Textures/button.png");
        if (File.Exists("Textures/button_hover.png"))
            hoverButtonImage = Image.FromFile("Textures/button_hover.png");

        //create UI controls
        Font buttonFont = new Font("Segoe UI", 14, FontStyle.Bold);

        // helper to create a textured button
        Button CreateMenuButton(string text)
        {
            var b = new Button
            {
                Text = text,
                Font = buttonFont,
                Size = new Size(200, 60),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                TextImageRelation = TextImageRelation.ImageAboveText,
                BackColor = Color.Transparent,
                BackgroundImageLayout = ImageLayout.Stretch
            };
            b.FlatAppearance.BorderSize = 0;
            if (defaultButtonImage != null)
            {
                b.BackgroundImage = defaultButtonImage;
            }

            // hover effects
            if (hoverButtonImage != null)
            {
                b.MouseEnter += (s, e) => { b.BackgroundImage = hoverButtonImage; };
                b.MouseLeave += (s, e) => { b.BackgroundImage = defaultButtonImage; };
            }
            return b;
        }

        btnStart = CreateMenuButton("Start Game");
        btnStart.Click += StartGame;

        btnControls = CreateMenuButton("Controls");
        btnControls.Click += ShowControls;

        btnChangeSkin = CreateMenuButton("Change Skin");
        btnChangeSkin.Click += ChangeSkin;

        btnChangeMap = CreateMenuButton("Change Map");
        btnChangeMap.Click += ChangeMap;

        btnPreviewMap = CreateMenuButton("Preview Map");
        btnPreviewMap.Click += PreviewMap;

        btnQuit = CreateMenuButton("Quit");
        btnQuit.Click += (s, e) => Application.Exit();

        // layout
        buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            Width = 220,
            BackColor = Color.Transparent,
            AutoScroll = false,
            Padding = new Padding(0)
        };
        buttonPanel.Controls.AddRange(new Control[] { btnStart, btnControls, btnChangeSkin, btnChangeMap, btnPreviewMap, btnQuit });

        // smaller vertical spacing
        btnStart.Margin = new Padding(0, 0, 0, 10);
        btnControls.Margin = new Padding(0, 0, 0, 10);
        btnChangeSkin.Margin = new Padding(0, 0, 0, 10);
        btnChangeMap.Margin = new Padding(0, 0, 0, 10);
        btnPreviewMap.Margin = new Padding(0, 0, 0, 10);
        btnQuit.Margin = new Padding(0);

        this.Controls.Add(buttonPanel);

        // initial positioning of the panel
        RepositionPanel();

        // respond to resize to keep panel centered vertically
        this.Resize += (s, e) => RepositionPanel();
    }

    // Button event-handlers
    private void StartGame(object? sender, EventArgs e)
    {
        // Hide menu and open the main game window.
        this.Hide();
        using (var gameForm = new ClientApp())
        {
            gameForm.ShowDialog();
        }
        // When the game window closes, quit the whole application.
        Application.Exit();
    }

    private void ShowControls(object? sender, EventArgs e)
    {
        const string controlsText =
            "Controls:\n" +
            "W/A/S/D - Move\n" +
            "Q/E/Y/X - Diagonal Move\n" +
            "Mouse-Wheel - Zoom\n" +
            "L - Look\n" +
            "C - Collect\n" +
            "F - Finish\n" +
            "G - Back to Main Menu\n" +
            "R - Center on Player\n" +
            "F1 - Algorithmic BFS Solve\n" +
            "F2 - Algorithmic A* Solve (WARNING: slower)\n" +
            "F3 - Reveal whole map";

        MessageBox.Show(controlsText, "Game Controls", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ChangeSkin(object? sender, EventArgs e)
    {
        int skinCount = Wins.Read() switch
        {
            < 1 => 1, // at least one skin available
            1 or 2 => 2, // 1 or 2 wins unlocks 2 skins
            3 or 4 => 3, // 3 or 4 wins unlocks 3 skins
            > 4 and < 10 => 4, // 5 to 9 wins unlocks 4 skins
            > 9 => 5 // 10 or more wins unlocks all 5 skins
        };
        skin++;
        if (skin > skinCount) skin = 1;

        TextureManager.ChangeSkin(skin);

        // provide quick feedback in button text
        btnChangeSkin.Text = $"Skin {skin}";

        // invalidate the form to update the UI
        this.Invalidate();
    }

    private void ChangeMap(object? sender, EventArgs e)
    {
        //# Dialog.CreateGenericDialog("Change Map not available", "This Feature is under Construction right now", "OK", this);
        //# return;
        map++;
        if (map > 4) map = 1; // cycle through maps 1 to 4
        switch (map)
        {
            case 1:
                GV.CurrentMap = GV.MAP_1;
                btnChangeMap.Text = "Map 1";
                break;
            case 2:
                GV.CurrentMap = GV.MAP_2;
                btnChangeMap.Text = "Map 2";
                break;
            case 3:
                GV.CurrentMap = GV.MAP_3;
                btnChangeMap.Text = "Map 3";
                break;
            case 4:
                GV.CurrentMap = GV.MAP_4;
                btnChangeMap.Text = "Map 4";
                break;
            default:
                GV.CurrentMap = GV.MAP_1; // fallback
                break;
        }
    }

    private void PreviewMap(object? sender, EventArgs e)
    {
        // Show a preview of the current map
        string previewPath = Path.Combine("Textures", GV.CurrentSkin.ToString(), $"map_{GV.CurrentMap}.png");
        if (File.Exists(previewPath))
        {
            using (var previewForm = new Form())
            {
                previewForm.Icon = this.Icon;
                previewForm.Text = "Map Preview";
                previewForm.Size = new Size(800, 600);
                previewForm.StartPosition = FormStartPosition.CenterScreen;

                PictureBox pictureBox = new PictureBox
                {
                    Dock = DockStyle.Fill,
                    Image = Image.FromFile(previewPath),
                    SizeMode = PictureBoxSizeMode.Zoom
                };
                previewForm.Controls.Add(pictureBox);

                previewForm.ShowDialog();
            }
        }
        else
        {
            MessageBox.Show("No preview available for this map.", "Preview Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // anti-flicker: ensure windows uses composited style
    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_COMPOSITED = 0x02000000;
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_COMPOSITED;
            return cp;
        }
    }

    // crisp background painting
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (backgroundImage == null)
        {
            base.OnPaintBackground(e);
            return;
        }

        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.None;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;

        // maintain aspect ratio while covering entire form
        float formRatio = (float)Width / Height;
        float imgRatio = (float)backgroundImage.Width / backgroundImage.Height;

        Rectangle dest;
        if (imgRatio > formRatio)
        {
            int drawHeight = Height;
            int drawWidth = (int)(drawHeight * imgRatio);
            int offsetX = (Width - drawWidth) / 2;
            dest = new Rectangle(offsetX, 0, drawWidth, drawHeight);
        }
        else
        {
            int drawWidth = Width;
            int drawHeight = (int)(drawWidth / imgRatio);
            int offsetY = (Height - drawHeight) / 2;
            dest = new Rectangle(0, offsetY, drawWidth, drawHeight);
        }

        g.DrawImage(Image.FromFile(Path.Combine("Textures", GV.CurrentSkin.ToString(), "main_menu.png")), dest);
    }

    private void RepositionPanel()
    {
        if (buttonPanel == null) return;
        int x = 30; // fixed left margin
        int y = (this.ClientSize.Height - buttonPanel.PreferredSize.Height) / 2;
        buttonPanel.Location = new Point(x, Math.Max(y, 0));
        buttonPanel.Height = buttonPanel.PreferredSize.Height; // ensure correct height
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Invalidate();
    }
}
